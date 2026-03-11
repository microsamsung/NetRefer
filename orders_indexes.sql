CREATE INDEX IX_Orders_CreatedOn
ON Orders(CreatedOn);

CREATE INDEX IX_Orders_CustomerId
ON Orders(CustomerId);

CREATE INDEX IX_Orders_CreatedOn_CustomerId
ON Orders(CreatedOn, CustomerId)
INCLUDE(Id);