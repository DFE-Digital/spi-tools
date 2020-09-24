using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.HistoricalDataPreparer.Domain.Gias;
using Dfe.Spi.HistoricalDataPreparer.Domain.Registry;
using Dfe.Spi.HistoricalDataPreparer.Domain.Translation;
using Dfe.Spi.HistoricalDataPreparer.Domain.Ukrlp;
using Serilog;

namespace Dfe.Spi.HistoricalDataPreparer.Application.ChangeProcessing
{
    public class RegistryChangeProcessor : ChangeProcessor
    {
        private const string EntityTypeManagementGroup = "management-group";
        private const string EntityTypeLearningProvider = "learning-provider";
        private const string LinkTypeSynonym = "synonym";
        private const string LinkTypeManagementGroup = "ManagementGroup";

        private readonly IPreparedRegistryRepository _preparedRegistryRepository;
        private readonly IPreparedGiasRepository _preparedGiasRepository;
        private readonly IPreparedUkrlpRepository _preparedUkrlpRepository;
        private readonly ITranslation _translation;
        private readonly ILogger _logger;

        public RegistryChangeProcessor(
            IPreparedRegistryRepository preparedRegistryRepository,
            IPreparedGiasRepository preparedGiasRepository,
            IPreparedUkrlpRepository preparedUkrlpRepository,
            ITranslation translation,
            ILogger logger)
        {
            _preparedRegistryRepository = preparedRegistryRepository;
            _preparedGiasRepository = preparedGiasRepository;
            _preparedUkrlpRepository = preparedUkrlpRepository;
            _translation = translation;
            _logger = logger;
        }

        public async Task<RegisteredEntity[]> ProcessChangesForDeltasAsync(
            DateTime date,
            Establishment[] changedEstablishments,
            Group[] changedGroups,
            LocalAuthority[] changedLocalAuthorities,
            Provider[] changedProviders,
            CancellationToken cancellationToken)
        {
            var allChanges = new List<RegisteredEntity>();

            allChanges.AddRange(await ProcessManagementGroupChangesAsync(changedGroups, changedLocalAuthorities, date, cancellationToken));
            allChanges.AddRange(await ProcessEstablishmentChangesAsync(changedEstablishments, date, cancellationToken));
            allChanges.AddRange(await ProcessProviderChangesAsync(changedProviders, date, cancellationToken));

            return allChanges
                .GroupBy(re => re.Id)
                .Select(g => g.Last())
                .ToArray();
        }

        private async Task<List<RegisteredEntity>> ProcessManagementGroupChangesAsync(
            Group[] changedGroups,
            LocalAuthority[] changedLocalAuthorities,
            DateTime date,
            CancellationToken cancellationToken)
        {
            // Build new entities
            var entities = new List<LinkedEntity>();

            foreach (var group in changedGroups)
            {
                var entity = await MapToEntityAsync(group, cancellationToken);
                entities.Add(entity);
            }

            foreach (var localAuthority in changedLocalAuthorities)
            {
                var entity = await MapToEntityAsync(localAuthority, cancellationToken);
                entities.Add(entity);
            }

            // Process changes
            var changes = new List<RegisteredEntity>();

            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var previous = await _preparedRegistryRepository.GetRegisteredEntityAsync(entity.Type, entity.SourceSystemName,
                    entity.SourceSystemId, date, cancellationToken);
                var latest = new RegisteredEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = entity.EntityType,
                    ValidFrom = date,
                    Entities = new[] {entity},
                    Links = previous?.Links,
                };

                if (previous != null)
                {
                    previous.ValidTo = date;
                    await _preparedRegistryRepository.StoreRegisteredEntity(previous, date, cancellationToken);
                    changes.Add(previous);
                }

                await _preparedRegistryRepository.StoreRegisteredEntity(latest, date, cancellationToken);
                changes.Add(latest);

                _logger.Information(
                    $"{i} of {entities.Count} changed management groups: Stored {{SourceSystemName}} {{SourceSystemId}} in registry as {{ChangeType}}",
                    entity.SourceSystemName, entity.SourceSystemId, previous == null ? "new" : "updated");
            }

            return changes;
        }

        private async Task<List<RegisteredEntity>> ProcessEstablishmentChangesAsync(
            Establishment[] changedEstablishments,
            DateTime date,
            CancellationToken cancellationToken)
        {
            var changes = new List<RegisteredEntity>();

            for (var i = 0; i < changedEstablishments.Length; i++)
            {
                var establishment = changedEstablishments[i];
                var entity = await MapToEntityAsync(establishment, date, cancellationToken);
                var previous = await _preparedRegistryRepository.GetRegisteredEntityAsync(entity.Type, entity.SourceSystemName,
                    entity.SourceSystemId, date, cancellationToken);
                var latest = new RegisteredEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = entity.EntityType,
                    ValidFrom = date,
                    Entities = new[] {entity},
                };

                if (previous != null)
                {
                    previous.ValidTo = date;
                    await _preparedRegistryRepository.StoreRegisteredEntity(previous, date, cancellationToken);
                    changes.Add(previous);

                    latest.Entities =
                        latest.Entities
                            .Concat(previous.Entities.Where(e => e.SourceSystemName != entity.SourceSystemName))
                            .ToArray();
                    latest.Links = previous.Links;
                }
                else
                {
                    var managementGroup = await _preparedRegistryRepository.GetRegisteredEntityAsync(EntityTypeManagementGroup,
                        SourceSystemNames.GetInformationAboutSchools, entity.ManagementGroupCode, date, cancellationToken);
                    if (managementGroup != null)
                    {
                        managementGroup.Links = (managementGroup.Links ?? new Link[0])
                            .Concat(new[]
                            {
                                new Link
                                {
                                    EntityType = entity.EntityType,
                                    SourceSystemName = entity.SourceSystemName,
                                    SourceSystemId = entity.SourceSystemId,
                                    LinkedAt = DateTime.Now,
                                    LinkedBy = "HistoricalDataPreparer",
                                    LinkedReason = "Matching management group code",
                                    LinkType = LinkTypeManagementGroup,
                                },
                            })
                            .ToArray();
                        await _preparedRegistryRepository.StoreRegisteredEntity(managementGroup, date, cancellationToken);
                        changes.Add(managementGroup);

                        latest.Links = new[]
                        {
                            new Link
                            {
                                EntityType = EntityTypeManagementGroup,
                                SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                                SourceSystemId = entity.ManagementGroupCode,
                                LinkedAt = DateTime.Now,
                                LinkedBy = "HistoricalDataPreparer",
                                LinkedReason = "Matching management group code",
                                LinkType = LinkTypeManagementGroup,
                            },
                        };
                    }
                }

                if (latest.Entities.Length == 1 && establishment.Ukprn.HasValue)
                {
                    var ukrlpEntity = await _preparedRegistryRepository.GetRegisteredEntityAsync(EntityTypeLearningProvider,
                        SourceSystemNames.UkRegisterOfLearningProviders, establishment.Ukprn.Value.ToString(), date, cancellationToken);
                    if (ukrlpEntity != null)
                    {
                        if (ukrlpEntity.ValidFrom == date)
                        {
                            await _preparedRegistryRepository.DeleteRegisteredEntity(ukrlpEntity.Id, cancellationToken);
                        }
                        else
                        {
                            ukrlpEntity.ValidTo = date;
                            await _preparedRegistryRepository.StoreRegisteredEntity(ukrlpEntity, date, cancellationToken);
                        }

                        latest.Entities = latest.Entities.Concat(ukrlpEntity.Entities).ToArray();
                        foreach (var linkedEntity in latest.Entities)
                        {
                            if (string.IsNullOrEmpty(linkedEntity.LinkedBy))
                            {
                                linkedEntity.LinkedAt = DateTime.UtcNow;
                                linkedEntity.LinkedBy = "HistoricalDataPreparer";
                                linkedEntity.LinkedReason = "Matching UKPRN";
                                linkedEntity.LinkType = LinkTypeSynonym;
                            }
                        }
                    }
                }

                await _preparedRegistryRepository.StoreRegisteredEntity(latest, date, cancellationToken);
                changes.Add(latest);

                _logger.Information(
                    $"{i} of {changedEstablishments.Length} changed establishments: Stored {{SourceSystemId}} in registry as {{ChangeType}}",
                    entity.SourceSystemId, previous == null ? "new" : "updated");
            }

            return changes;
        }

        private async Task<List<RegisteredEntity>> ProcessProviderChangesAsync(
            Provider[] changedProviders,
            DateTime date,
            CancellationToken cancellationToken)
        {
            var changes = new List<RegisteredEntity>();

            for (var i = 0; i < changedProviders.Length; i++)
            {
                var provider = changedProviders[i];
                var entity = await MapToEntityAsync(provider, cancellationToken);
                var previous = await _preparedRegistryRepository.GetRegisteredEntityAsync(entity.Type, entity.SourceSystemName,
                    entity.SourceSystemId, date, cancellationToken);
                var latest = new RegisteredEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = entity.EntityType,
                    ValidFrom = date,
                    Entities = new[] {entity},
                };

                if (previous != null)
                {
                    previous.ValidTo = date;
                    await _preparedRegistryRepository.StoreRegisteredEntity(previous, date, cancellationToken);
                    changes.Add(previous);

                    latest.Entities =
                        latest.Entities
                            .Concat(previous.Entities.Where(e => e.SourceSystemName != entity.SourceSystemName))
                            .ToArray();
                    latest.Links = previous.Links;

                    await _preparedRegistryRepository.StoreRegisteredEntity(latest, date, cancellationToken);
                    changes.Add(latest);
                }

                if (latest.Entities.Length == 1 && entity.Urn.HasValue)
                {
                    var giasEntity = await _preparedRegistryRepository.GetRegisteredEntityAsync(EntityTypeLearningProvider,
                        SourceSystemNames.GetInformationAboutSchools, entity.Urn.Value.ToString(), date, cancellationToken);
                    if (giasEntity != null)
                    {
                        if (giasEntity.ValidFrom == date)
                        {
                            await _preparedRegistryRepository.DeleteRegisteredEntity(giasEntity.Id, cancellationToken);
                        }
                        else
                        {
                            giasEntity.ValidTo = date;
                            await _preparedRegistryRepository.StoreRegisteredEntity(giasEntity, date, cancellationToken);
                        }

                        latest.Entities = latest.Entities.Concat(giasEntity.Entities).ToArray();
                        foreach (var linkedEntity in latest.Entities)
                        {
                            if (string.IsNullOrEmpty(linkedEntity.LinkedBy))
                            {
                                linkedEntity.LinkedAt = DateTime.UtcNow;
                                linkedEntity.LinkedBy = "HistoricalDataPreparer";
                                linkedEntity.LinkedReason = "Matching URN";
                                linkedEntity.LinkType = LinkTypeSynonym;
                            }
                        }
                    }
                }

                await _preparedRegistryRepository.StoreRegisteredEntity(latest, date, cancellationToken);
                changes.Add(latest);

                _logger.Information(
                    $"{i} of {changedProviders.Length} changed providers: Stored {{SourceSystemId}} in registry as {{ChangeType}}",
                    entity.SourceSystemId, previous == null ? "new" : "updated");
            }

            return changes;
        }

        private async Task<LinkedEntity> MapToEntityAsync(Group group, CancellationToken cancellationToken)
        {
            var translatedGroupType = await _translation.TranslateEnumValueAsync(
                EnumerationNames.ManagementGroupType, SourceSystemNames.GetInformationAboutSchools, group.GroupType, cancellationToken);
            var code = $"{translatedGroupType}-{group.Uid}";

            return new LinkedEntity
            {
                EntityType = EntityTypeManagementGroup,
                SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                SourceSystemId = code,
                Name = group.GroupName,
                ManagementGroupType = translatedGroupType,
                ManagementGroupCode = code,
                ManagementGroupId = group.Uid.ToString(),
                ManagementGroupUkprn = group.Ukprn,
                ManagementGroupCompaniesHouseNumber = group.CompaniesHouseNumber,
            };
        }

        private async Task<LinkedEntity> MapToEntityAsync(LocalAuthority localAuthority, CancellationToken cancellationToken)
        {
            var translatedGroupType = await _translation.TranslateEnumValueAsync(
                EnumerationNames.ManagementGroupType, SourceSystemNames.GetInformationAboutSchools, LocalAuthority.ManagementGroupType, cancellationToken);
            var code = $"{translatedGroupType}-{localAuthority.Code}";

            return new LinkedEntity
            {
                EntityType = EntityTypeManagementGroup,
                SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                SourceSystemId = code,
                Name = localAuthority.Name,
                ManagementGroupType = translatedGroupType,
                ManagementGroupCode = code,
                ManagementGroupId = localAuthority.Code.ToString(),
            };
        }

        private async Task<LinkedEntity> MapToEntityAsync(Provider provider, CancellationToken cancellationToken)
        {
            var translatedStatus = await _translation.TranslateEnumValueAsync(EnumerationNames.ProviderStatus, SourceSystemNames.UkRegisterOfLearningProviders,
                provider.ProviderStatus, cancellationToken);

            var urnVerification = provider.Verifications.SingleOrDefault(vd =>
                vd.Authority.Equals("DfE (Schools Unique Reference Number)", StringComparison.InvariantCultureIgnoreCase));
            var dfeNumberVerification = provider.Verifications.SingleOrDefault(vd =>
                vd.Authority.Equals("DfE (Schools Unique Reference Number)", StringComparison.InvariantCultureIgnoreCase));
            var companiesHouseNumberVerification = provider.Verifications.SingleOrDefault(vd =>
                vd.Authority.Equals("Companies House", StringComparison.InvariantCultureIgnoreCase));
            var charitiesCommissionNumberVerification = provider.Verifications.SingleOrDefault(vd =>
                vd.Authority.Equals("Charity Commission", StringComparison.InvariantCultureIgnoreCase));

            return new LinkedEntity
            {
                EntityType = EntityTypeLearningProvider,
                SourceSystemName = SourceSystemNames.UkRegisterOfLearningProviders,
                SourceSystemId = provider.UnitedKingdomProviderReferenceNumber.ToString(),
                Name = provider.ProviderName,
                Status = translatedStatus,
                Urn = urnVerification != null ? (long?) long.Parse(urnVerification.Id) : null,
                Ukprn = provider.UnitedKingdomProviderReferenceNumber,
                CompaniesHouseNumber = companiesHouseNumberVerification?.Id,
                CharitiesCommissionNumber = charitiesCommissionNumberVerification?.Id,
                DfeNumber = dfeNumberVerification?.Id,
            };
        }

        private async Task<LinkedEntity> MapToEntityAsync(Establishment establishment, DateTime date, CancellationToken cancellationToken)
        {
            // Translate
            var translatedType = await _translation.TranslateEnumValueAsync(EnumerationNames.ProviderType, SourceSystemNames.GetInformationAboutSchools,
                establishment.EstablishmentTypeGroup.Code, cancellationToken);
            var translatedSubType = await _translation.TranslateEnumValueAsync(EnumerationNames.ProviderSubType, SourceSystemNames.GetInformationAboutSchools,
                establishment.TypeOfEstablishment.Code, cancellationToken);
            var translatedStatus = await _translation.TranslateEnumValueAsync(EnumerationNames.ProviderStatus, SourceSystemNames.GetInformationAboutSchools,
                establishment.EstablishmentStatus.Code, cancellationToken);

            // Build entity provider details
            var entity = new LinkedEntity
            {
                EntityType = EntityTypeLearningProvider,
                SourceSystemName = SourceSystemNames.GetInformationAboutSchools,
                SourceSystemId = establishment.Urn.ToString(),
                Name = establishment.EstablishmentName,
                Type = translatedType,
                SubType = translatedSubType,
                Status = translatedStatus,
                OpenDate = establishment.OpenDate,
                CloseDate = establishment.CloseDate,
                Urn = establishment.Urn,
                Ukprn = establishment.Ukprn,
                Uprn = establishment.Uprn,
                CompaniesHouseNumber = establishment.CompaniesHouseNumber,
                CharitiesCommissionNumber = establishment.CharitiesCommissionNumber,
                AcademyTrustCode = establishment.Trusts?.Code,
                DfeNumber = $"{establishment.LA.Code}/{establishment.EstablishmentNumber}",
                LocalAuthorityCode = establishment.LA.Code,
            };

            // Add management group details
            var group = establishment.Trusts != null && !string.IsNullOrEmpty(establishment.Trusts.Code)
                ? await _preparedGiasRepository.GetGroupAsync(long.Parse(establishment.Trusts.Code), date, cancellationToken)
                : null;
            if (group == null && establishment.Federations != null && !string.IsNullOrEmpty(establishment.Federations.Code))
            {
                group = await _preparedGiasRepository.GetGroupAsync(long.Parse(establishment.Federations.Code), date, cancellationToken);
            }

            if (group != null)
            {
                var translatedGroupType = await _translation.TranslateEnumValueAsync(
                    EnumerationNames.ManagementGroupType, SourceSystemNames.GetInformationAboutSchools, group.GroupType, cancellationToken);

                entity.ManagementGroupType = translatedGroupType;
                entity.ManagementGroupId = group.Uid.ToString();
                entity.ManagementGroupCode = $"{translatedGroupType}-{group.Uid}";
                entity.ManagementGroupUkprn = group.Ukprn;
                entity.ManagementGroupCompaniesHouseNumber = group.CompaniesHouseNumber;
            }
            else
            {
                var translatedGroupType = await _translation.TranslateEnumValueAsync(
                    EnumerationNames.ManagementGroupType, SourceSystemNames.GetInformationAboutSchools, LocalAuthority.ManagementGroupType, cancellationToken);

                entity.ManagementGroupType = translatedGroupType;
                entity.ManagementGroupId = establishment.LA.Code;
                entity.ManagementGroupCode = $"{translatedGroupType}-{establishment.LA.Code}";
            }

            // Return
            return entity;
        }
    }
}