-- 권장 PRAGMA (앱에서도 동일 적용)
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;

BEGIN IMMEDIATE TRANSACTION;

-- 주문 원장
CREATE TABLE IF NOT EXISTS Orders (
  id            TEXT PRIMARY KEY,                -- Guid("N")
  ts            TEXT NOT NULL,                   -- ISO8601(UTC)
  market        TEXT NOT NULL,                   -- "KRW-BTC"
  side          TEXT NOT NULL,                   -- "Buy" | "Sell"
  type          TEXT NOT NULL,                   -- "Market" | "Limit" | "Stop" | "StopLimit"
  tif           TEXT NOT NULL DEFAULT 'GTC',     -- "GTC" | "IOC" | "FOK"
  price         NUMERIC NULL,                    -- 지정가/스톱가격
  stop_price    NUMERIC NULL,
  quantity      NUMERIC NOT NULL,
  filled_qty    NUMERIC NOT NULL DEFAULT 0,
  status        TEXT NOT NULL,                   -- "New" | "PartiallyFilled" | "Filled" | "Canceled" | "Rejected"
  note          TEXT NULL
);

-- 체결
CREATE TABLE IF NOT EXISTS Fills (
  id        TEXT PRIMARY KEY,                    -- Guid("N")
  order_id  TEXT NOT NULL REFERENCES Orders(id) ON DELETE CASCADE,
  ts        TEXT NOT NULL,                       -- ISO8601(UTC)
  market    TEXT NOT NULL,
  side      TEXT NOT NULL,                       -- "Buy" | "Sell"
  price     NUMERIC NOT NULL,
  quantity  NUMERIC NOT NULL,
  fee       NUMERIC NOT NULL DEFAULT 0
);

-- 보유
CREATE TABLE IF NOT EXISTS Holdings (
  asset   TEXT PRIMARY KEY,                      -- "KRW","BTC"
  free    NUMERIC NOT NULL DEFAULT 0,
  locked  NUMERIC NOT NULL DEFAULT 0
);

-- 포트폴리오 스냅샷
CREATE TABLE IF NOT EXISTS PortfolioSnapshots (
  ts             TEXT PRIMARY KEY,               -- ISO8601(UTC)
  equity         NUMERIC NOT NULL,
  cash           NUMERIC NOT NULL,
  unrealized_pnl NUMERIC NOT NULL DEFAULT 0,
  realized_pnl   NUMERIC NOT NULL DEFAULT 0
);

-- 관심종목
CREATE TABLE IF NOT EXISTS Watchlist (
  symbol  TEXT PRIMARY KEY,                      -- "KRW-BTC"
  sort    INTEGER NOT NULL DEFAULT 0,
  note    TEXT NULL
);

-- 인덱스
CREATE INDEX IF NOT EXISTS idx_orders_market_ts      ON Orders(market, ts);
CREATE INDEX IF NOT EXISTS idx_orders_status_ts      ON Orders(status, ts);
CREATE INDEX IF NOT EXISTS idx_fills_order_ts        ON Fills(order_id, ts);
CREATE INDEX IF NOT EXISTS idx_fills_market_ts       ON Fills(market, ts);
CREATE INDEX IF NOT EXISTS idx_snapshots_ts          ON PortfolioSnapshots(ts);

-- 스키마 버전
PRAGMA user_version = 1;

COMMIT;