using eCommerce.DataAccessLayer.Entities;
using eCommerce.DataAccessLayer.RepositoryContracts;
using MongoDB.Driver;

namespace eCommerce.DataAccessLayer.Repositories;

public class OrdersRepository : IOrdersRepository
{
    private readonly IMongoCollection<Order> _orders;
    private readonly string collectionName = "orders";

    public OrdersRepository(IMongoDatabase mongoDatabase)
    {
        _orders = mongoDatabase.GetCollection<Order>(collectionName);
    }


    public async Task<Order?> AddOrder(Order order)
    {
        order.OrderID = Guid.NewGuid();

        await _orders.InsertOneAsync(order);
        return order;
    }


    public async Task<bool> DeleteOrder(Guid orderID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);

        DeleteResult deleteResult = await _orders.DeleteOneAsync(filter);

        return deleteResult.DeletedCount > 0;
    }


    public async Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        return await _orders.Find(filter).FirstOrDefaultAsync();
    }


    public async Task<IEnumerable<Order>> GetOrders()
    {
        return (await _orders.FindAsync(Builders<Order>.Filter.Empty)).ToList();
    }


    public async Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        return (await _orders.FindAsync(filter)).ToList();
    }


    public async Task<Order?> UpdateOrder(Order order)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, order.OrderID);

        var result = await _orders.ReplaceOneAsync(filter, order);
        if (result.MatchedCount == 0)
            return null;

        return order;
    }
}