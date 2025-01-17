using Caculate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Caculate.Configurations
{
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.ToTable("Members");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Name).IsUnique();
            builder.Property(x => x.Name).HasMaxLength(250);
        }
    }
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);            
            builder.Property(x => x.CreatedDate).IsRequired();
            builder.Property(x => x.TotalMoney).IsRequired();
            builder.HasOne(x => x.Payer).WithMany().HasForeignKey(x => x.PayerId).OnDelete(DeleteBehavior.Cascade);
        }
    }
    public class OrderParticipantConfiguration : IEntityTypeConfiguration<OrderParticipant>
    {
        public void Configure(EntityTypeBuilder<OrderParticipant> builder)
        {
            builder.ToTable("OrderParticipants");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Money).IsRequired();
            builder.Property(x => x.CreatedDate).IsRequired();
            builder.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Order).WithMany(x => x.Participants).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
