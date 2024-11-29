using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAppApi.Data;
using System.Security.Claims;
using QuizAppApi.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace QuizAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("user")] 
        public IActionResult GetUser() 
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return Ok(new { UserId = userId }); 
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroupsOfTheUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var userGroups = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Include(gm => gm.Group)
                .Select(gm => gm.Group)
                .ToListAsync();

            if (userGroups == null)
                return NotFound("You're not a member of any groups!");

         
            return Ok(userGroups);
        }

        [HttpGet("GetGroupById/{groupId}")]
        public async Task<IActionResult> GetGroupById(Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
                return NotFound("Group not found!");

            return Ok(group);
        }

        [HttpGet("{groupId}/members")]
        public async Task<IActionResult> GetAllMembers(Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;  
            if (userId == null)
                return Unauthorized("User Id not found!");

            var isMember = await _context.GroupMembers
                .Where(gm => (gm.GroupId == groupId) && (gm.UserId == userId))
                .FirstOrDefaultAsync() != null;

            var isAdmin = await _context.Groups
                .Where(g => g.GroupId == groupId && g.AdminId == userId)
                .FirstOrDefaultAsync() != null;

            if (!isMember || !isAdmin)
                return Unauthorized("You're not a member of the group!");

            var groupMembers = await _context.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => new
                {
                    gm.GroupMemberId,
                    gm.User.FullName,
                    gm.UserId
                })
                .ToListAsync();

            return Ok(groupMembers);
        }

        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroup createGroup)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var group = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = createGroup.groupName,
                AdminId = userId
            };

            _context.Groups.Add(group);

            var adminUserMember = await _context.Users.FindAsync(userId);   
            var adminMember = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = userId,
                User = adminUserMember
            };
            
            _context.GroupMembers.Add(adminMember);
            await _context.SaveChangesAsync();

            return Ok(group);
        }

        [HttpPut("TransferAdminRights")]
        public async Task<IActionResult> TransferAdminRights([FromForm] Guid groupId, [FromForm] string newAdminId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(userId == null)
                return Unauthorized("User Id not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
                return NotFound("Group not found!");

            var isAdmin = await _context.Groups
                .Where(g => (g.GroupId == group.GroupId &&  g.AdminId == userId))
                .FirstOrDefaultAsync() !=  null;
            if (!isAdmin)
                return Unauthorized("You are not the admin!");

            group.AdminId = newAdminId;
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();

            return Ok(group.AdminId);
        }

        [HttpPost("AddMember")]
        public async Task<IActionResult> AddMember([FromForm] Guid groupId, [FromForm] string newUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var newMember = await _context.Users.FindAsync(newUserId);
            if (newMember == null)
                return NotFound("User not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return Unauthorized("Only the group admin is allowed to perform this action!");

            var isMember = await _context.GroupMembers
                .Where(gm => ( gm.GroupId == groupId && gm.User.Id == newUserId))
                .FirstOrDefaultAsync() != null;
            if (isMember)
                return BadRequest("The user is already a member of the group!");

            var newUser = new GroupMember
            {
                GroupId = groupId,
                UserId = newUserId,
                User = newMember
            };
            _context.GroupMembers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(newUser);
        }

        [HttpPost("RemoveMember/{groupMemberId}")]
        public async Task<IActionResult> RemoveMember(Guid groupMemberId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User Id not found!");

            var groupMember = await _context.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupMemberId == groupMemberId);
            if (groupMember == null) return NotFound("Member not found!");

            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => (g.GroupId == groupMember.GroupId));
            if (group == null) return NotFound("Group not found!");

            if (group.AdminId == groupMember.UserId)
                return BadRequest("Please pass on the admin rights to others!");

            if (group.AdminId != userId) return BadRequest("Only the group admin is allowed to perform this action!");

            _context.GroupMembers.Remove(groupMember);
            await _context.SaveChangesAsync();

            return Ok(group);
        }

        [HttpDelete("DeleteGroup/{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User Id not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null) return NotFound("Group not found!");

            if (group.AdminId != userId)
                return BadRequest("Only the group admin is allowed to perform this task!");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return Ok("The group has been deleted successfully!");
        }
    }

    public class CreateGroup
    {
        public string groupName { get; set; } = string.Empty;
    }
}
