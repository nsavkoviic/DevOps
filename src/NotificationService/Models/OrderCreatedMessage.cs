namespace NotificationService.Models;

public class OrderCreatedMessage
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
