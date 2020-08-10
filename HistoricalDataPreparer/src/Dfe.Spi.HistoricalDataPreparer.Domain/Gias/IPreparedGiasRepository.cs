using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.HistoricalDataPreparer.Domain.Gias
{
    public interface IPreparedGiasRepository
    {
        Task<Establishment> GetEstablishmentAsync(long urn, DateTime date, CancellationToken cancellationToken);
        Task StoreEstablishmentAsync(Establishment establishment, DateTime date, CancellationToken cancellationToken);
        
        Task<Group> GetGroupAsync(long uid, DateTime date, CancellationToken cancellationToken);
        Task StoreGroupAsync(Group group, DateTime date, CancellationToken cancellationToken);
        
        Task<LocalAuthority> GetLocalAuthorityAsync(int laCode, DateTime date, CancellationToken cancellationToken);
        Task StoreLocalAuthorityAsync(LocalAuthority localAuthority, DateTime date, CancellationToken cancellationToken);
    }
}