using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SympNet.WebApi.Data;
using SympNet.WebApi.Models;
using System.Security.Claims;

namespace SympNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogPostsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BlogPostsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/blogposts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BlogPost>>> GetBlogPosts([FromQuery] string? category = null)
    {
        var query = _db.BlogPosts.Where(b => b.IsPublished);
        
        if (!string.IsNullOrEmpty(category) && category != "Tous")
        {
            query = query.Where(b => b.Category == category);
        }
        
        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    // GET: api/blogposts/featured
    [HttpGet("featured")]
    public async Task<ActionResult<BlogPost>> GetFeaturedPost()
    {
        var post = await _db.BlogPosts
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.Views)
            .ThenByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();
            
        if (post == null) return NotFound();
        return post;
    }

    // GET: api/blogposts/doctor
    [HttpGet("doctor")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<IEnumerable<BlogPost>>> GetDoctorBlogPosts()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
        
        var userId = Guid.Parse(userIdClaim);
        return await _db.BlogPosts
            .Where(b => b.AuthorId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    // GET: api/blogposts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<BlogPost>> GetBlogPost(int id)
    {
        var blogPost = await _db.BlogPosts.FindAsync(id);

        if (blogPost == null)
        {
            return NotFound();
        }

        blogPost.Views++;
        await _db.SaveChangesAsync();

        return blogPost;
    }

    // POST: api/blogposts
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<BlogPost>> PostBlogPost(BlogPost blogPost)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
        
        blogPost.AuthorId = Guid.Parse(userIdClaim);
        
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.UserId == blogPost.AuthorId);
        if (doctor != null)
        {
            blogPost.AuthorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
        }

        blogPost.CreatedAt = DateTime.UtcNow;
        blogPost.UpdatedAt = DateTime.UtcNow;

        _db.BlogPosts.Add(blogPost);
        await _db.SaveChangesAsync();

        return CreatedAtAction("GetBlogPost", new { id = blogPost.Id }, blogPost);
    }

    // PUT: api/blogposts/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> PutBlogPost(int id, BlogPost blogPost)
    {
        if (id != blogPost.Id)
        {
            return BadRequest();
        }

        var existing = await _db.BlogPosts.FindAsync(id);
        if (existing == null) return NotFound();
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || existing.AuthorId != Guid.Parse(userIdClaim))
            return Unauthorized();

        existing.Title = blogPost.Title;
        existing.Content = blogPost.Content;
        existing.Summary = blogPost.Summary;
        existing.ImageUrl = blogPost.ImageUrl;
        existing.Category = blogPost.Category;
        existing.ReadingTime = blogPost.ReadingTime;
        existing.IsPublished = blogPost.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        _db.Entry(existing).State = EntityState.Modified;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BlogPostExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/blogposts/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DeleteBlogPost(int id)
    {
        var blogPost = await _db.BlogPosts.FindAsync(id);
        if (blogPost == null)
        {
            return NotFound();
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || blogPost.AuthorId != Guid.Parse(userIdClaim))
            return Unauthorized();

        _db.BlogPosts.Remove(blogPost);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool BlogPostExists(int id)
    {
        return _db.BlogPosts.Any(e => e.Id == id);
    }
}
