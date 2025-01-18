using Caculate.DataContext;
using Caculate.Entities;
using Microsoft.EntityFrameworkCore;

namespace Caculate
{
    public class MemberService : IMemberService
    {
        private readonly CaculateDbContext _dbContext;

        public MemberService(CaculateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> AddNewMember(Member member)
        {
            await _dbContext.AddAsync(member);
            var result = await _dbContext.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteMember(Guid memberId)
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

        public async Task<List<Member>> GetAllMembers()
        {
            var result = await _dbContext.Members.ToListAsync();
            return result;
        }

        public async Task<Member?> GetMemberById(Guid memberId)
        {
            var result = await _dbContext.Members.FirstOrDefaultAsync(m => m.Id == memberId);
            return result;
        }

        public Task<Member?> GetMemberByName(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateMember(Member member)
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

        public async Task<bool> IsExistName(string name)
        {
            var result = await _dbContext.Members.AnyAsync(m => m.Name == name);
            return result;
        }

    }
}
