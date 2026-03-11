CREATE VIEW vw_OrderReport
AS
SELECT
    o.Id,
    o.CreatedOn,
    c.Name
FROM Orders o
INNER JOIN Customers c
    ON c.Id = o.CustomerId;

--    SELECT
--    o.Id,
--    o.CreatedOn,
--    c.Name
--FROM Orders o
--LEFT JOIN Customers c
--    ON c.Id = o.CustomerId
--WHERE o.CreatedOn >= @From
--AND o.CreatedOn < DATEADD(DAY,1,@To);