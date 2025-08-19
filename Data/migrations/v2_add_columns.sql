PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;
PRAGMA user_version;  -- 현재 버전 읽기

BEGIN IMMEDIATE;

-- v1 → v2: Orders 컬럼 추가 (SQLite는 ADD COLUMN만 지원)
ALTER TABLE Orders ADD COLUMN client_order_id TEXT NULL;      -- 클라이언트에서 부여한 원본 ID
ALTER TABLE Orders ADD COLUMN strategy_tag    TEXT NULL;      -- 전략/그룹 태그
ALTER TABLE Orders ADD COLUMN last_updated    TEXT NULL;      -- 갱신 시각(ISO8601 UTC)

-- 기존 레코드 초기화: last_updated = ts
UPDATE Orders SET last_updated = ts WHERE last_updated IS NULL;

-- 유니크 보장(선택): client_order_id 가 비어있지 않은 행만 유니크
-- 부분 인덱스는 SQLite 3.8.0+에서 지원
CREATE UNIQUE INDEX IF NOT EXISTS ux_orders_client_order_id
ON Orders(client_order_id)
WHERE client_order_id IS NOT NULL AND client_order_id <> '';

PRAGMA user_version = 2;
COMMIT;
