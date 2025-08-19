PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;
PRAGMA user_version;  -- 현재 버전 읽기

BEGIN IMMEDIATE;

-- 조회 패턴 보완 인덱스
CREATE INDEX IF NOT EXISTS idx_orders_ts                ON Orders(ts);
CREATE INDEX IF NOT EXISTS idx_orders_strategy_tag_ts   ON Orders(strategy_tag, ts);
CREATE INDEX IF NOT EXISTS idx_orders_side_ts           ON Orders(side, ts);
CREATE INDEX IF NOT EXISTS idx_fills_ts                 ON Fills(ts);

-- (참고) v1에서 이미 존재:
-- idx_orders_market_ts(market, ts), idx_orders_status_ts(status, ts),
-- idx_fills_order_ts(order_id, ts), idx_fills_market_ts(market, ts),
-- idx_snapshots_ts(ts)

PRAGMA user_version = 3;
COMMIT;
