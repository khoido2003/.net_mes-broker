using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=MessageBroker.db"));

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// create topic 
app.MapPost("api/topics", async (AppDbContext context, Topic topic) =>
{
    await context.Topics.AddAsync(topic);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{topic.Id}", topic);
});

// Return all topic
app.MapGet("api/topics", async (AppDbContext context) =>
{
    var topics = context.Topics.ToListAsync();

    return Results.Ok(topics);
});

// Publish messages
app.MapPost("api/topics/{id}/messages", async (AppDbContext context, int id, Message message) =>
{

    Console.WriteLine("=-=" + message.TopicMessage);
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    Console.WriteLine("TOPICS: " + topics);

    if (!topics)
    {
        return Results.NotFound("Topics not found!");
    }

    var subs = context.Subscriptions.Where(s => s.TopicId == id);

    if (subs.Count() == 0)
    {
        return Results.NotFound("There are no subsription for this topic");
    }
    foreach (var sub in subs)
    {
        Message msg = new Message
        {
            TopicMessage = message.TopicMessage,
            SubscriptionId = sub.Id,
            ExpiresAfter = message.ExpiresAfter,
            MessageStatus = message.MessageStatus
        };

        await context.Messages.AddAsync(msg);
    }
    await context.SaveChangesAsync();
    return Results.Ok("Message has been published");
});

// Create subscription
app.MapPost("api/topics/{id}/subscriptions", async (AppDbContext context, int id, Subscription sub) =>
{
    bool topics = await context.Topics.AnyAsync(t => t.Id == id);
    if (!topics)
    {
        return Results.NotFound("Topics not found!");
    }

    sub.TopicId = id;
    await context.Subscriptions.AddAsync(sub);
    await context.SaveChangesAsync();

    return Results.Created($"api/topics/{id}/subscripios/{sub.Id}", sub);
});

// Get subscriber Messages
app.MapGet("api/subscription/{id}/messages", async (AppDbContext context, int id) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);

    if (!subs)
    {
        return Results.NotFound("Subscription not found");
    }

    var messages = context.Messages.Where(m => m.SubscriptionId == id && m.MessageStatus != "SENT");

    if (messages.Count() == 0)
    {
        return Results.NotFound("No New Messages");
    }

    foreach (var msg in messages)
    {
        msg.MessageStatus = "REQUESTED";

    }

    await context.SaveChangesAsync();
    return Results.Ok(messages);

    // Ack messages for scubscriber

});

app.MapPost("api/subscription/{id}/messages", async (AppDbContext context, int id, int[] confs) =>
{
    bool subs = await context.Subscriptions.AnyAsync(s => s.Id == id);

    if (!subs)
    {
        return Results.NotFound("Subscription not found");
    }

    if (confs.Length <= 0)
    {
        return Results.BadRequest();
    }

    int count = 0;
    foreach (int i in confs)
    {
        var msg = context.Messages.FirstOrDefault();

        if (msg != null)
        {
            msg.MessageStatus = "SENT";
            await context.SaveChangesAsync();
            count++;


        }
    }
    return Results.Ok($"Acknowleged {count}/{confs.Length} messages");
});




app.Run();



