using Caculate.DataContext;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace Caculate
{
    public class MemberService : IMemberService
    {
        private readonly CaculateDbContext _dbContext;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MemberService(CaculateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> AddNewMember(Member member)
        {
            try
            {
                await _dbContext.AddAsync(member);
                var result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when add new member: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMember(Guid memberId)
        {
            try
            {
                var member = await GetMemberById(memberId);
                if (member == null)
                {
                    return false;
                }
                _dbContext.Members.Remove(member);
                var result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error when delete member: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Member>> GetAllMembers()
        {
            try
            {
                var result = await _dbContext.Members.ToListAsync();
                return result;
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error when get list all members: {ex.Message}");
                return new List<Member>();
            }
        }

        public async Task<Member?> GetMemberById(Guid memberId)
        {
            try
            {
                var result = await _dbContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when get member by id: {memberId}, error: {ex.Message}");
                return null;
            }
        }

        public async Task<Member?> GetMemberByName(string name)
        {
            try
            {
                var result = await _dbContext.Members.FirstOrDefaultAsync(x => x.Name == name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when get member by name: {name}, error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateMember(Member member)
        {
            try
            {
                var isExistMember = await _dbContext.Members.AnyAsync(x => x.Name == member.Name && x.Id != member.Id);
                if (isExistMember)
                {
                    return false;
                }
                _dbContext.Members.Update(member);
                var result = await _dbContext.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when edit member: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsExistName(string name)
        {
            try
            {
                var result = await _dbContext.Members.AnyAsync(m => m.Name == name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error when check exist member: {ex.Message}");
                return false;
            }
        }

    }
}
