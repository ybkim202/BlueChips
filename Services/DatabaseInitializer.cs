using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlueChips.Services {
    public sealed class DatabaseInitializer {
        private readonly SettingsProvider _settings;
        public DatabaseInitializer(SettingsProvider settings) => _settings = settings;

        public void Initialize() => InitializeAsync().GetAwaiter().GetResult();

        public async Task InitializeAsync() {
            var cfg = _settings.Get();
            var dbPath = cfg.Database!.Path;

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            using var conn = new SqliteConnection(cs);
            await conn.OpenAsync().ConfigureAwait(false);   // Changed to use ConfigureAwait(false) for better performance in library code

            using (var cmd = conn.CreateCommand()) {
                cmd.CommandText =
                    "PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
                using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) { } // WAL 결과 드레인
            }

            var ver = await GetUserVersionAsync(conn).ConfigureAwait(false);

            Console.WriteLine($"[DB] user_version={ver}");

            var baseDir = AppContext.BaseDirectory;
            var schemaPath = Path.Combine(baseDir, "Data", "schema_v1.sql");
            var v2Path = Path.Combine(baseDir, "Data", "migrations", "v2_add_columns.sql");
            var v3Path = Path.Combine(baseDir, "Data", "migrations", "v3_indexes.sql");

            Console.WriteLine($"[DB] BaseDir={baseDir}");
            Console.WriteLine($"[DB] DbPath={dbPath} (exists={File.Exists(dbPath)})");
            Console.WriteLine($"[DB] schema_v1={schemaPath} (exists={File.Exists(schemaPath)})");
            Console.WriteLine($"[DB] v2={v2Path} (exists={File.Exists(v2Path)})");
            Console.WriteLine($"[DB] v3={v3Path} (exists={File.Exists(v3Path)})");


            if (ver == 0) {
                Console.WriteLine("[DB] RUN schema_v1.sql");
                await ExecuteSqlFromFile(conn, schemaPath).ConfigureAwait(false);
                ver = await GetUserVersionAsync(conn).ConfigureAwait(false);
                Console.WriteLine($"[DB] -> after v1 user_version={ver}");
            }
            if (ver == 1) {
                Console.WriteLine("[DB] RUN v2_add_columns.sql");
                await ExecuteSqlFromFile(conn, v2Path).ConfigureAwait(false);
                ver = await GetUserVersionAsync(conn).ConfigureAwait(false);
                Console.WriteLine($"[DB] -> after v2 user_version={ver}");
            }
            if (ver == 2) {
                Console.WriteLine("[DB] RUN v3_indexes.sql");
                await ExecuteSqlFromFile(conn, v3Path).ConfigureAwait(false);
                ver = await GetUserVersionAsync(conn).ConfigureAwait(false);
                Console.WriteLine($"[DB] -> after v3 user_version={ver}");
            }

            Console.WriteLine("[DB] initialize done");
        }


        private static async Task<int> GetUserVersionAsync(SqliteConnection conn) {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version;";
            var obj = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return Convert.ToInt32(obj);
        }


        private static string StripSqlComments(string sql) {
            // 1) 블록 주석 제거: /* ... */
            sql = Regex.Replace(sql, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            // 2) 라인 주석 제거: -- ... (줄 끝까지)
            var sb = new StringBuilder(sql.Length);
            using var sr = new StringReader(sql.Replace("\r", ""));
            string? line;
            while ((line = sr.ReadLine()) != null) {
                var t = line.TrimStart();
                if (t.StartsWith("--")) continue; // 라인 자체가 주석이면 스킵
                sb.AppendLine(line);
            }
            return sb.ToString();
        }


        /// <summary>
        /// 스크립트에 BEGIN/COMMIT이 있으면 바깥에서 트랜잭션을 열지 않고 그대로 실행한다.
        /// (schema_v1.sql, v2, v3가 여기에 해당)
        /// </summary>
        private static async Task ExecuteSqlFromFile(SqliteConnection conn, string path) {
            if (!File.Exists(path)) throw new FileNotFoundException("SQL file not found", path);
            Console.WriteLine($"[DB] exec file: {Path.GetFileName(path)}");

            // 0) 안전 읽기
            var raw = await SafeReadAllTextAsync(path).ConfigureAwait(false);
            Console.WriteLine($"[DB] file size={raw.Length}");

            // 1) 주석 제거
            var cleaned = StripSqlComments(raw);

            // 2) 문장 분리
            var stmts = cleaned
                .Split(';')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            Console.WriteLine($"[DB] statements={stmts.Count}");

            int i = 0;
            foreach (var stmt in stmts) {
                i++;
                var head = (stmt.Split('\n', '\t', ' ').FirstOrDefault() ?? "").ToUpperInvariant();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = stmt + ";";

                try {
                    if (head == "BEGIN" || head == "COMMIT" || head == "END") {
                        Console.WriteLine($"[DB] #{i} {head}");
                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        continue;
                    }

                    if (head == "PRAGMA") {
                        bool isAssign = stmt.Contains('=');
                        Console.WriteLine($"[DB] #{i} PRAGMA(assign={isAssign}) {stmt}");
                        if (isAssign) {
                            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                        else {
                            using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                            while (await r.ReadAsync().ConfigureAwait(false)) { }
                        }
                        continue;
                    }

                    if (head == "SELECT" || head == "WITH") {
                        Console.WriteLine($"[DB] #{i} {head}");
                        using var r = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                        while (await r.ReadAsync().ConfigureAwait(false)) { }
                        continue;
                    }

                    Console.WriteLine($"[DB] #{i} NonQuery {head}");
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
                catch (Exception ex) {
                    Console.WriteLine($"[DB] ERROR at #{i} ({head})\nSQL: {stmt}\n{ex}");
                    throw;
                }
            }
            Console.WriteLine($"[DB] exec done: {Path.GetFileName(path)}");
        }




        static async Task<string> SafeReadAllTextAsync(string path, int maxRetries = 5, int initialDelayMs = 50) {
            var delay = initialDelayMs;
            Console.WriteLine($"[DB] SafeReadAllTextAsync: {path} (maxRetries={maxRetries}, initialDelayMs={initialDelayMs})");
            for (int i = 0; i < maxRetries; i++) {
                Console.WriteLine($"[DB] Attempt {i + 1} to read file: {path}");
                try {
                    await using var fs = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        // 🔑 다른 프로세스가 읽거나 쓰고 있어도 가능한 한 읽어오도록 공유 플래그 확대
                        FileShare.ReadWrite | FileShare.Delete,
                        4096,
                        useAsync: true);

                    using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    
                    Console.WriteLine($"[DB] Successfully opened file: {path}");
                    return await sr.ReadToEndAsync().ConfigureAwait(false);
                }
                catch (IOException ex) {
                    // 잠금/공유 충돌 가능성 → 짧게 백오프 후 재시도
                    if (i == maxRetries - 1) throw new IOException($"Failed to read file after {maxRetries} retries: {path}", ex);
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay *= 2;
                }
                catch (UnauthorizedAccessException ex) {
                    // 드물게 디렉터리/권한/Exclusive lock 등
                    if (i == maxRetries - 1) throw new UnauthorizedAccessException($"Access denied when reading file: {path}", ex);
                    await Task.Delay(delay);
                    delay *= 2;
                }
            }
            throw new IOException($"Failed to read file: {path}");
        }
    }
}
