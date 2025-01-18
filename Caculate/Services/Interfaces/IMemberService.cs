using Caculate.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caculate
{
    public interface IMemberService
    {
        Task<bool> AddNewMember(Member member);
        Task<bool> UpdateMember(Member member);
        Task<bool> DeleteMember(Guid memberId);
        Task<Member?> GetMemberById(Guid memberId);
        Task<Member?> GetMemberByName(string name);
        Task<List<Member>> GetAllMembers();
        Task<bool> IsExistName(string name);
    }
}
