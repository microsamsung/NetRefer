Technical Test – Suggested Solutions 

Section 1 – Production Incident Debugging 

Possible null sources: 

- Request model may be null 

- Order.Items collection may be null 

- User session object 

- Database result 

- Dependency injected services 

 

 

public ActionResult Submit(OrderViewModel model) 

{ 

    if (model == null) 

        return BadRequest("Invalid request."); 

  

    if (model.Items == null || !model.Items.Any()) 

        return BadRequest("Order must contain at least one item."); 

  

    var user = Session["User"] as User; 

  

    if (user == null) 

        return RedirectToAction("Login", "Account"); 

  

    if (user.Customer == null) 

        return BadRequest("Customer information missing."); 

  

    try 

    { 

        var customerId = user.Customer.Id; 

  

        var orderId = _service.CreateOrder(customerId, model.Items); 

  

        return RedirectToAction("Success", new { id = orderId }); 

    } 

    catch (Exception ex) 

    { 

        _logger.LogError(ex, "Order submission failed"); 

  

        return StatusCode(500, "Internal server error"); 

    } 

} 

public class OrderViewModel 

{ 

[Required] 

public List<OrderItem> Items { get; set; } 

} 

 

 

if (!ModelState.IsValid) 

{ 

return View(model); 

} 

 

_logger.LogError(ex, 

"Order creation failed for CustomerId {CustomerId}", 

user.Customer.Id); 

 

All details  can be added in logger . CustomerId 

OrderId 

RequestId 

Timestamp 

Correlation ID helps trace the request across logs. 

 var correlationId = HttpContext.TraceIdentifier; 

_logger.LogInformation( 

"Submit order started | CorrelationId: {CorrelationId}", 

correlationId); 

 

Final code :  

 

public ActionResult Submit(OrderViewModel model) 

{ 

var correlationId = HttpContext.TraceIdentifier; 

 

_logger.LogInformation("Submit order request received. CorrelationId {CorrelationId}", correlationId); 

 

if (model == null || model.Items == null || !model.Items.Any()) 

{ 

_logger.LogWarning("Invalid order model. CorrelationId {CorrelationId}", correlationId); 

return BadRequest("Invalid order request."); 

} 

 

var user = Session["User"] as User; 

 

if (user?.Customer == null) 

{ 

_logger.LogWarning("User session invalid. CorrelationId {CorrelationId}", correlationId); 

return RedirectToAction("Login", "Account"); 

} 

 

try 

{ 

var orderId = _service.CreateOrder(user.Customer.Id, model.Items); 

 

_logger.LogInformation( 

"Order created successfully. OrderId {OrderId}, CorrelationId {CorrelationId}", 

orderId, 

correlationId); 

 

return RedirectToAction("Success", new { id = orderId }); 

} 

catch (Exception ex) 

{ 

_logger.LogError(ex, 

"Error creating order. CorrelationId {CorrelationId}", 

correlationId); 

 

return StatusCode(500, "Internal server error"); 

} 

} 

Section 2 – SQL Troubleshooting 

Common reason dates missing: 

- Using BETWEEN with datetime 

- Time component excluded 

 

Bad filter: 

WHERE OrderDate BETWEEN '2024-01-01' AND '2024-01-31' 

 

Better: 

WHERE OrderDate >= '2024-01-01' 

AND OrderDate < '2024-02-01' 

 

Problem 

Some orders were missing in reports due to incorrect date filtering using BETWEEN. 

 

Example: 

BETWEEN '2024-01-01 00:00:00' 

AND '2024-01-31 00:00:00' 

 

Records on the last day after midnight were excluded. 

 

Fix 

Use half-open interval: 

 

CreatedOn >= @From 

AND CreatedOn < DATEADD(DAY,1,@To) 

 

### Index Improvements 

Indexes added on: 

 

Orders.CreatedOn  

Orders.CustomerId  

 

These improve filtering and join performance. 

 

### Join Risk 

Using INNER JOIN may hide orders if the related customer record is missing. 

A LEFT JOIN may be safer for reporting scenarios. 

 

### Date-Time Risk 

DATETIME precision may exclude records due to time components. 

DATETIME2 is recommended for better precision. 

Use UTC date 

Section 3 – WebForms Grid Disappears 

Root cause: 

Grid is rebound only on first load using: 

 

if(!IsPostBack) 

BindGrid(); 

 

After button click PostBack occurs and grid loses data. 

 

Solution: 

Store dataset in ViewState or rebind correctly. 

 

protected void Page_Load(object sender, EventArgs e) 

{ 

    if(!IsPostBack) 

        LoadData(); 

} 

 

protected void Button_Click(object sender, EventArgs e) 

{ 

    ProcessAction(); 

    LoadData(); 

} 

The issue occurs because BindGrid() is executed on every Page_Load, including postbacks. 

This causes the grid to reload before postback values are fully restored, sometimes resulting in empty data. 

  

The solution is to bind the grid only when the page loads initially using !IsPostBack. 

Filtering and paging events then trigger BindGrid() intentionally. 

  

Paging is supported using GridView PageIndexChanging, and database load can be reduced by caching results or implementing server-side paging. 

Section 4 – Payment API 

Problems: 

- Network timeout 

- Payment succeeds but DB update fails 

- Duplicate callbacks 

 

Improvements: 

- Idempotency key 

- Retry policy 

- Transactional update 

 

Example Retry (Polly): 

 

Policy 

.Handle<HttpRequestException>() 

.WaitAndRetryAsync(3,i => TimeSpan.FromSeconds(i)); 

 

Logging: 

- Payment request 

- Provider response 

- Callback result 

 

Potential issues include network failures, incorrect status code checks, 

timeouts, and duplicate requests. 

  

Improvements include validating IsSuccessStatusCode, implementing retries using Polly, 

handling timeouts, and introducing idempotency keys to avoid duplicate payments. 

  

Additionally, webhook callbacks and structured logging should be used 

to ensure system consistency and observability. 

 

Section 5 – Azure Function Event Hub 

EventHub guarantees at-least-once delivery. 

 

Risks: 

- duplicate events 

- partial failure 

 

Idempotent design: 

 

Store processed EventId in table: 

 

ProcessedEvents 

EventId (PK) 

ProcessedDate 

 

Before processing: 

check if exists. 

Azure Event Hub uses at-least-once delivery, meaning events may be delivered multiple times. 

To handle duplicates, events should be processed idempotently using a ProcessedEvents table. 

  

Each event should be checked before processing and stored after successful execution. 

  

Failures should be handled per event with logging and retries to avoid data loss. 

  

Structured logging should include EventId, OrderId, and correlation identifiers to support observability. 

public async Task Run( 

EventData[] events, 

ILogger log) 

{ 

    foreach(var e in events) 

    { 

        var msg = 

        JsonSerializer.Deserialize<OrderPaidEvent>( 

        e.Body.ToArray()); 

  

        try 

        { 

            if(await _repo.EventProcessed(msg.EventId)) 

            { 

                log.LogInformation( 

                "Duplicate event {EventId}", 

                msg.EventId); 

  

                continue; 

            } 

  

            await _repo.MarkPaid(msg.OrderId); 

  

            await _repo.StoreProcessedEvent(msg.EventId); 

  

            log.LogInformation( 

            "Order updated {OrderId}", 

            msg.OrderId); 

        } 

        catch(Exception ex) 

        { 

            log.LogError( 

            ex, 

            "Failure Order {OrderId}", 

            msg.OrderId); 

  

            throw; 

        } 

    } 

} 

Section 7 – GraphQL Performance 

N+1 problem: 

public IQueryable<Order> GetOrders([Service] AppDb

The N+1 problem occurs because GraphQL loads orders first and then executes one query per order to fetch payments. This can cause major performance issues. 
Using DataLoader batching reduces queries to a single batch query. Pagination should be added to avoid loading large datasets. Because resolvers execute independently, 
data consistency may vary, so transactions or caching strategies may be required.

var payments =
await db.Payments
.Where(p => orderIds.Contains(p.OrderId))
.ToListAsync();

GraphQL resolvers execute independently, so data may change between field resolutions, causing inconsistent snapshots. 
This is a trade-off between performance and strong consistency. 
Solutions include transactions, batching, caching, or accepting eventual consistency depending on business requirements.

SECTION 8: Angular – UI shows Paid then reverts to Pending:

The UI updates optimistically before backend confirmation, causing status flickering when the server still returns Pending. A safer approach is to reload the order 
after payment instead of updating locally. When using REST and GraphQL together, cache invalidation or refetching is needed to avoid stale data. Proper error handling 
should include failure messages and retry mechanisms.

SECTION 9: Logical reasoning – how you break down a production problem:

SECTION 9 – Production Problem Analysis
1. Possible causes grouped logically
Frontend layer

Optimistic UI update showing Paid before backend confirmation.

GraphQL cache returning stale order status.

UI refreshing before async payment processing completes.

Backend layer

Payment succeeded but order status update failed.

Transaction rollback after payment success.

REST command updated order but GraphQL read model not synchronized.

Async processing layer

Azure Function processing payment events with delay.

Event duplication causing incorrect state transitions.

Event processing failure leaving order in Pending state.

Data consistency layer

Eventual consistency between write and read models.

Read replica lag.

Missing idempotency allowing conflicting updates.

2. What to check first and why

Priority should be checks that quickly identify the failure domain.

First checks:

Database order status history

Verify if status actually changed.

Determines UI vs backend issue.

Payment transaction logs

Confirm payment success timestamp.

Azure Function logs

Check if payment event processed successfully.

CorrelationId tracing

Track full order lifecycle.

Reason:

These checks quickly isolate whether the issue is UI presentation, backend logic, or asynchronous processing.

3. Evidence for three causes
Cause: GraphQL stale data

Confirm:

SQL shows Paid.

GraphQL shows Pending.

Cache refresh fixes issue.

Rule out:

Both show same status.

Cause: Event processing delay

Confirm:

Event processed minutes after payment.

Logs show retry or delay.

Rule out:

Event processed immediately.

Cause: Payment update failure

Confirm:

Payment provider shows success.

No order update log exists.

Rule out:

Order updated correctly in DB.

4. Minimal reproducible scenario

Steps:

Create test order.

Execute payment.

Immediately query order via:

REST

GraphQL

Direct SQL.

Capture:

OrderId
Payment timestamp
Event processing timestamp
UI refresh timestamp

Repeat under controlled timing.

Goal:

Identify smallest sequence that reproduces the inconsistency.

5. Temporary mitigation (no code change)

Best operational mitigation:

Reduce GraphQL cache duration.

Add monitoring for Pending orders after payment.

Manual reconciliation job for stuck orders.

Customer support workflow to verify payment status.

Best single mitigation:

Reduce cache lifetime or force read consistency to minimize incorrect UI states until a permanent fix is deployed.




 
