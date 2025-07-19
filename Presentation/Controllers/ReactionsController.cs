using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.Repositories.Reaction;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/reactions")]
    public class ReactionsController : BaseApiController
    {
        private readonly IReactionRepo _reactionRepo;
        private readonly IChatClientNotifier _chatClientNotifier;
        private readonly ILogger<ReactionsController> _logger;

        public ReactionsController(IReactionRepo reactionRepo, IChatClientNotifier chatClientNotifier, ILogger<ReactionsController> logger)
        {
            _reactionRepo = reactionRepo;
            _chatClientNotifier = chatClientNotifier;
            _logger = logger;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddReaction([FromBody] ReactionRequest request)
        {
            try
            {
                // Check if reaction already exists
                var existingReaction = await _reactionRepo.GetByMessageUserEmojiAsync(request.message_id, request.user_id, request.emoji);
                if (existingReaction != null)
                {
                    return BadRequestResponse("User has already reacted with this emoji");
                }

                // Create new reaction
                var reaction = new MessageReaction
                {
                    message_id = request.message_id,
                    user_id = request.user_id,
                    emoji = request.emoji,
                    reacted_at = DateTime.UtcNow
                };

                reaction.id = await _reactionRepo.InsertAsync(reaction);

                _logger.LogInformation("Broadcasting reaction add - MessageId: {MessageId}, UserId: {UserId}, Emoji: {Emoji}", 
                    request.message_id, request.user_id, request.emoji);

                // Broadcast reaction to all clients
                await _chatClientNotifier.SendReactionAsync(request.message_id, reaction);

                return OkResponse(reaction, "Reaction added successfully");
            }
            catch (Exception ex)
            {
                return InternalErrorResponse($"Error adding reaction: {ex.Message}");
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveReaction([FromBody] ReactionRequest request)
        {
            try
            {
                var success = await _reactionRepo.RemoveReactionAsync(request.message_id, request.user_id, request.emoji);
                
                if (!success)
                {
                    return BadRequestResponse("Reaction not found");
                }

                _logger.LogInformation("Broadcasting reaction remove - MessageId: {MessageId}, UserId: {UserId}, Emoji: {Emoji}", 
                    request.message_id, request.user_id, request.emoji);

                // Broadcast reaction removal to all clients
                await _chatClientNotifier.RemoveReactionAsync(request.message_id, request.user_id, request.emoji);

                return OkResponse(new { success = true }, "Reaction removed successfully");
            }
            catch (Exception ex)
            {
                return InternalErrorResponse($"Error removing reaction: {ex.Message}");
            }
        }
    }
}