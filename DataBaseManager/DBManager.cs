using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq; // Required for .Any() if you choose to use it
using System.Threading.Tasks;

/// <summary>
/// Defines the different document types that can be stored in the database.
/// The names of the enum members are used as collection names by default.
/// Example: DocumentType.User will point to the "User" collection.
/// Adjust this in the GetCollectionName method if needed.
/// </summary>
public enum DocumentType
{
    User,       // Example: Maps to "User" collection
    TimeEntry, // Example: Maps to "TimeEntry" collection
    Project,   // Example: Maps to "Project" collection
    Employee,  // Example: Maps to "Employee" collection
    ProjectTimeBooking, // Example: Maps to "ProjectTimeBooking" collection
    LeaveRequest, // Example: Maps to "LeaveRequest" collection
    SickNote // Example: Maps to "SickNote" collection
    // Add more document types as needed, following the same pattern
}

public class DBManager
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the DBManager class.
    /// </summary>
    /// <param name="dbName">The name of the database to use.</param>
    /// <param name="connectionString">The connection string to the MongoDB instance.</param>
    public DBManager(string dbName, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(dbName))
            throw new ArgumentNullException(nameof(dbName), "Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be empty.");

        _mongoClient = new MongoClient(connectionString);
        _database = _mongoClient.GetDatabase(dbName);
    }

    /// <summary>
    /// Gets the MongoDB collection name based on the DocumentType.
    /// By default, it uses the enum member's name (e.g., DocumentType.User -> "User").
    /// This method can be customized to implement specific naming conventions
    /// (e.g., lowercase, plural forms: "users", "products").
    /// </summary>
    /// <param name="docType">The type of the document.</param>
    /// <returns>The name of the collection.</returns>
    private string GetCollectionName(DocumentType docType)
    {
        // Current implementation: Enum name is collection name.
        // Example for customization (lowercase and plural):
        // return docType.ToString().ToLower() + "s";
        return docType.ToString();
    }

    /// <summary>
    /// Provides direct access to a MongoDB collection for the given document type.
    /// </summary>
    public IMongoCollection<TDocument> GetCollection<TDocument>(DocumentType docType) where TDocument : class
    {
        return _database.GetCollection<TDocument>(GetCollectionName(docType));
    }

    #region CREATE Operations

    /// <summary>
    /// Creates a new document in the specified collection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="document">The document to create.</param>
    public async Task CreateAsync<TDocument>(DocumentType docType, TDocument document) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        await collection.InsertOneAsync(document);
    }

    /// <summary>
    /// Inserts multiple documents into the specified collection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the documents.</typeparam>
    /// <param name="docType">The type of the documents, determining the collection.</param>
    /// <param name="documents">The collection of documents to insert. The driver will throw an ArgumentNullException if this is null. An empty collection is a no-op.</param>
    public async Task InsertManyAsync<TDocument>(DocumentType docType, IEnumerable<TDocument> documents) where TDocument : class
    {
        // The MongoDB driver's InsertManyAsync will throw an ArgumentNullException if 'documents' is null.
        // If 'documents' is an empty collection, it will result in a no-op (no database call), which is generally fine.
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        await collection.InsertManyAsync(documents);
    }

    #endregion

    #region READ Operations

    /// <summary>
    /// Retrieves a document by its ID from the specified collection.
    /// Assumes the ID is a string representing an ObjectId.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="id">The ID of the document (as a string, expected in ObjectId format).</param>
    /// <returns>The found document, or null if it doesn't exist or the ID is invalid.</returns>
    public async Task<TDocument> GetByIdAsync<TDocument>(DocumentType docType, string id) where TDocument : class
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            throw new ArgumentException("Invalid ID format. Must be a 24-digit hex string.", nameof(id));
        }
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var filter = Builders<TDocument>.Filter.Eq("_id", objectId);
        return await collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves all documents from the specified collection.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <returns>A list of all documents in the collection.</returns>
    public async Task<List<TDocument>> GetAllAsync<TDocument>(DocumentType docType) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        return await collection.Find(Builders<TDocument>.Filter.Empty).ToListAsync();
    }

    /// <summary>
    /// Finds documents in the specified collection that match the filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="filter">The filter definition.</param>
    /// <returns>A list of found documents.</returns>
    public async Task<List<TDocument>> FindAsync<TDocument>(DocumentType docType, FilterDefinition<TDocument> filter) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        return await collection.Find(filter).ToListAsync();
    }

    #endregion

    #region UPDATE Operations

    /// <summary>
    /// Replaces an existing document by its ID with a new document.
    /// Assumes the ID is a string representing an ObjectId.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="id">The ID of the document to replace (as a string, expected in ObjectId format).</param>
    /// <param name="document">The new document to replace the old one.</param>
    /// <returns>True if the document was successfully replaced, otherwise false.</returns>
    public async Task<bool> ReplaceByIdAsync<TDocument>(DocumentType docType, string id, TDocument document) where TDocument : class
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            throw new ArgumentException("Invalid ID format. Must be a 24-digit hex string.", nameof(id));
        }
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var filter = Builders<TDocument>.Filter.Eq("_id", objectId);
        var result = await collection.ReplaceOneAsync(filter, document);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    /// <summary>
    /// Partially updates a single document matching the filter using the provided update definition.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="filter">The filter to select the document to update.</param>
    /// <param name="updateDefinition">The definition of the updates to perform.</param>
    /// <returns>True if at least one document was successfully updated, otherwise false.</returns>
    public async Task<bool> UpdateAsync<TDocument>(DocumentType docType, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> updateDefinition) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var result = await collection.UpdateOneAsync(filter, updateDefinition);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    /// <summary>
    /// Partially updates multiple documents matching the filter using the provided update definition.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="filter">The filter to select the documents to update.</param>
    /// <param name="updateDefinition">The definition of the updates to perform.</param>
    /// <returns>The number of modified documents if successful and acknowledged, otherwise -1.</returns>
    public async Task<long> UpdateManyAsync<TDocument>(DocumentType docType, FilterDefinition<TDocument> filter, UpdateDefinition<TDocument> updateDefinition) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var result = await collection.UpdateManyAsync(filter, updateDefinition);
        return result.IsAcknowledged ? result.ModifiedCount : -1L;
    }


    #endregion

    #region DELETE Operations

    /// <summary>
    /// Deletes a document by its ID from the specified collection.
    /// Assumes the ID is a string representing an ObjectId.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="id">The ID of the document to delete (as a string, expected in ObjectId format).</param>
    /// <returns>True if the document was successfully deleted, otherwise false.</returns>
    public async Task<bool> DeleteByIdAsync<TDocument>(DocumentType docType, string id) where TDocument : class
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            throw new ArgumentException("Invalid ID format. Must be a 24-digit hex string.", nameof(id));
        }
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var filter = Builders<TDocument>.Filter.Eq("_id", objectId);
        var result = await collection.DeleteOneAsync(filter);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    /// <summary>
    /// Deletes multiple documents matching the filter.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    /// <param name="docType">The type of the document, determining the collection.</param>
    /// <param name="filter">The filter to select the documents to delete.</param>
    /// <returns>The number of deleted documents if successful and acknowledged, otherwise -1.</returns>
    public async Task<long> DeleteManyAsync<TDocument>(DocumentType docType, FilterDefinition<TDocument> filter) where TDocument : class
    {
        var collection = _database.GetCollection<TDocument>(GetCollectionName(docType));
        var result = await collection.DeleteManyAsync(filter);
        return result.IsAcknowledged ? result.DeletedCount : -1L;
    }

    #endregion
}