CREATE PROCEDURE sp_GetOrdersForReport
    @From DATETIME2,
    @To DATETIME2
AS
BEGIN

SELECT
    Id,
    CreatedOn,
    Name
FROM vw_OrderReport
WHERE CreatedOn >= @From
AND CreatedOn < DATEADD(DAY,1,@To)

END