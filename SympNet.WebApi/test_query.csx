
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=sympnet;Username=postgres;Password=yasmine");
using var db = new AppDbContext(optionsBuilder.Options);

var conversations = await db.Conversations
    .Select(c => new
    {
        c.Id,
        c.DoctorId,
        c.PatientId,
        LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
        MessageCount = c.Messages.Count()
    })
    .ToListAsync();

foreach(var c in conversations) {
    Console.WriteLine($"Conv {c.Id}: Messages={c.MessageCount}, LastMessage={c.LastMessage ?? "NULL"}");
}
Console.WriteLine("Done.");

