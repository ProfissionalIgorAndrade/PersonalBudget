-- Removes the duplicate "Transferência" categories created before the fix in
-- TransferTransactionCreationStrategy.
--
-- Existing transactions point at the duplicates, so they cannot simply be
-- deleted. Each household keeps one category, every transaction is repointed
-- at it, and the rest are removed.
--
-- READ BEFORE RUNNING. This has not been executed against any database -
-- there was no way to reach one from where it was written. Take a backup,
-- run it inside the transaction as written, and check the counts at each
-- step before committing.

BEGIN;

-- 1. What is there now. Expect one row per household with count > 1.
SELECT "HouseholdId", COUNT(*) AS duplicates
FROM categories
WHERE lower("Name") = lower('Transferência')
GROUP BY "HouseholdId"
ORDER BY duplicates DESC;

-- 2. The survivor per household: the one the application will now pick,
--    matching its ORDER BY Id.
CREATE TEMP TABLE transfer_keep AS
SELECT DISTINCT ON ("HouseholdId") "HouseholdId", "Id" AS keep_id
FROM categories
WHERE lower("Name") = lower('Transferência')
ORDER BY "HouseholdId", "Id";

-- 3. Everything being merged away.
CREATE TEMP TABLE transfer_drop AS
SELECT c."Id" AS drop_id, k.keep_id
FROM categories c
JOIN transfer_keep k ON k."HouseholdId" = c."HouseholdId"
WHERE lower(c."Name") = lower('Transferência')
  AND c."Id" <> k.keep_id;

-- 4. How many transactions are about to move. Note this number.
SELECT COUNT(*) AS transactions_to_repoint
FROM transactions t
JOIN transfer_drop d ON d.drop_id = t."CategoryId";

-- 5. Repoint them.
UPDATE transactions t
SET "CategoryId" = d.keep_id
FROM transfer_drop d
WHERE t."CategoryId" = d.drop_id;

-- 6. Nothing should reference the duplicates now. This must return 0.
SELECT COUNT(*) AS still_referencing
FROM transactions t
JOIN transfer_drop d ON d.drop_id = t."CategoryId";

-- 7. Remove them.
DELETE FROM categories c
USING transfer_drop d
WHERE c."Id" = d.drop_id;

-- 8. Should be exactly one per household.
SELECT "HouseholdId", COUNT(*) AS remaining
FROM categories
WHERE lower("Name") = lower('Transferência')
GROUP BY "HouseholdId";

-- If every check looks right:
--   COMMIT;
-- otherwise:
--   ROLLBACK;
ROLLBACK;
