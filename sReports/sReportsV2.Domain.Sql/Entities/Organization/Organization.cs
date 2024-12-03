using Newtonsoft.Json;
using sReportsV2.Common.Enums.DocumentPropertiesEnums;
using sReportsV2.Domain.Sql.Entities.Common;
using sReportsV2.Domain.Sql.EntitiesBase;
using sReportsV2.Domain.Sql.Entities.PersonnelTeamEntities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using sReportsV2.Common.Extensions;
using System;

namespace sReportsV2.Domain.Sql.Entities.OrganizationEntities
{
    public class Organization : Entity, IEditChildEntries<OrganizationIdentifier>, IEditChildEntries<OrganizationTelecom>, IEditChildEntries<OrganizationClinicalDomain>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Column("OrganizationId")]
        public int OrganizationId { get; set; }  
        [NotMapped]
        public List<int> Type { get; set; }
        public string TypesString
        {
            get
            {
                return this.Type == null || !this.Type.Any()
                           ? null
                           : JsonConvert.SerializeObject(this.Type);
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    this.Type = new List<int>();
                else
                    this.Type = JsonConvert.DeserializeObject<List<int>>(value);
            }

        }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string LogoUrl { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public string Impressum { get; set; }
        public int NumOfUsers { get; set; }
        public virtual int? OrganizationRelationId { get; set; }
        public int? OrganizationAddressId { get; set; }
        [ForeignKey("OrganizationAddressId")]
        public virtual OrganizationAddress OrganizationAddress { get; set; }
        [ForeignKey("OrganizationRelationId")]
        public virtual OrganizationRelation OrganizationRelation { get; set; }
        public virtual List<OrganizationClinicalDomain> ClinicalDomains { get; set; } = new List<OrganizationClinicalDomain>();
        public virtual List<OrganizationTelecom> Telecoms { get; set; }
        public virtual List<PersonnelTeamOrganizationRelation> PersonnelTeamOrganizationRelations { get; set; } = new List<PersonnelTeamOrganizationRelation>();
        public virtual List<OrganizationIdentifier> OrganizationIdentifiers { get; set; } = new List<OrganizationIdentifier>();
        public virtual List<OrganizationCommunicationEntity> OrganizationCommunicationEntities { get; set; }
        public string TimeZone { get; set; }
        public string TimeZoneOffset { get; set; }

        public void Copy(Organization copyFrom)
        {
            this.Alias = copyFrom.Alias;
            this.Description = copyFrom.Description;
            this.Email = copyFrom.Email;
            this.OrganizationId = copyFrom.OrganizationId;
            this.LogoUrl = copyFrom.LogoUrl;
            this.Name = copyFrom.Name;
            this.PrimaryColor = copyFrom.PrimaryColor;
            this.SecondaryColor = copyFrom.SecondaryColor;
            this.Type = copyFrom.Type;
            this.Impressum = copyFrom.Impressum;
            CopyAddress(copyFrom.OrganizationAddress);
            CopyEntries(copyFrom.Telecoms);
            CopyEntries(copyFrom.OrganizationIdentifiers);
            CopyEntries(copyFrom.ClinicalDomains);
            this.TimeZone = copyFrom.TimeZone;
            this.TimeZoneOffset = copyFrom.TimeZoneOffset;
            CopyRowVersion(copyFrom);
        }

        public void SetRelation(OrganizationRelation upcomingOrganizationRelation)
        {
            int parentId = upcomingOrganizationRelation.ParentId;

            if ((parentId == 0 && this.OrganizationRelation == null) 
                || (parentId == this.OrganizationRelation?.ParentId)
                )
            {
                return;
            }

            if(this.OrganizationRelation == null)
            {
                this.OrganizationRelation = upcomingOrganizationRelation;
            }
            else
            {
                if(parentId > 0)
                {
                    this.OrganizationRelation.ParentId = parentId;
                    this.OrganizationRelation.SetLastUpdate();
                }
                else
                {
                    this.OrganizationRelation.Delete(setLastUpdateProperty: false);
                    this.OrganizationRelationId = null;
                    this.OrganizationRelation = null;
                }
                
            }
        }

        public void SetClinicalDomains(List<int> clinicalDomains)
        {
            if (clinicalDomains == null || clinicalDomains.Count() == 0)
            {
                this.ClinicalDomains?.ForEach(x => x.Delete(setLastUpdateProperty: false));
                return;
            }

            foreach (OrganizationClinicalDomain organizationClinicalDomain in ClinicalDomains)
            {
                if (!clinicalDomains.Contains(organizationClinicalDomain.ClinicalDomainCD.Value))
                {
                    organizationClinicalDomain.Delete(setLastUpdateProperty: false);
                }
            }

            foreach (var clinicalDomain in clinicalDomains)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow.ConvertToOrganizationTimeZone();
                if (!this.ClinicalDomains.Any(x => x.IsActive() && x.ClinicalDomainCD == (int)clinicalDomain))
                {
                    this.ClinicalDomains.Add(new OrganizationClinicalDomain()
                    {
                        ClinicalDomainCD = (int)clinicalDomain,
                        OrganizationId = this.OrganizationId
                    });
                }
            }
        }

        private void CopyAddress(OrganizationAddress copyFrom)
        {
            if (copyFrom == null)
            {
                this.OrganizationAddress = null;
                return;
            }

            if (this.OrganizationAddress == null)
            {
                this.OrganizationAddress = new OrganizationAddress(copyFrom.CreatedById);
            }

            this.OrganizationAddress.Copy(copyFrom);
        }

        #region Edit Child entries

        public void CopyEntries(List<OrganizationIdentifier> upcomingEntries)
        {
            if (upcomingEntries != null)
            {
                DeleteExistingRemovedEntries(upcomingEntries);
                AddNewOrUpdateOldEntries(upcomingEntries);
            }
        }

        public void DeleteExistingRemovedEntries(List<OrganizationIdentifier> upcomingEntries)
        {
            foreach (var identifier in OrganizationIdentifiers)
            {
                var remainingIdentifier = upcomingEntries.Any(x => x.OrganizationIdentifierId == identifier.OrganizationIdentifierId);
                if (!remainingIdentifier)
                {
                    identifier.Delete();
                }
            }
        }

        public void AddNewOrUpdateOldEntries(List<OrganizationIdentifier> upcomingEntries)
        {
            foreach (var identifier in upcomingEntries)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow.ConvertToOrganizationTimeZone();
                if (identifier.OrganizationIdentifierId == 0)
                {
                    OrganizationIdentifiers.Add(identifier);
                }
                else
                {
                    var dbIdentifier = OrganizationIdentifiers.FirstOrDefault(x => x.OrganizationIdentifierId == identifier.OrganizationIdentifierId 
                        && x.IsActive());
                    if (dbIdentifier != null)
                    {
                        dbIdentifier.Copy(identifier);
                    }
                }
            }
        }

        public void CopyEntries(List<OrganizationTelecom> upcomingEntries)
        {
            if (upcomingEntries != null)
            {
                DeleteExistingRemovedEntries(upcomingEntries);
                AddNewOrUpdateOldEntries(upcomingEntries);
            }
        }

        public void DeleteExistingRemovedEntries(List<OrganizationTelecom> upcomingEntries)
        {
            foreach (var telecom in Telecoms)
            {
                var remainingTelecom = upcomingEntries.Any(x => x.OrganizationTelecomId == telecom.OrganizationTelecomId);
                if (!remainingTelecom)
                {
                    telecom.Delete();
                }
            }
        }

        public void AddNewOrUpdateOldEntries(List<OrganizationTelecom> upcomingEntries)
        {
            foreach (var organizationTelecom in upcomingEntries)
            {
                if (organizationTelecom.OrganizationTelecomId == 0)
                {
                    Telecoms.Add(organizationTelecom);
                }
                else
                {
                    var dbPatientTelecom = Telecoms.FirstOrDefault(x => x.OrganizationTelecomId == organizationTelecom.OrganizationTelecomId 
                        && x.IsActive());
                    if (dbPatientTelecom != null)
                    {
                        dbPatientTelecom.Copy(organizationTelecom);
                    }
                }
            }
        }

        public void CopyEntries(List<OrganizationClinicalDomain> upcomingEntries)
        {
            if (upcomingEntries != null)
            {
                DeleteExistingRemovedEntries(upcomingEntries);
                AddNewOrUpdateOldEntries(upcomingEntries);
            }
        }

        public void DeleteExistingRemovedEntries(List<OrganizationClinicalDomain> upcomingEntries)
        {
            foreach (var clinicalDomain in ClinicalDomains)
            {
                var remainingTelecom = upcomingEntries.Any(x => x.OrganizationClinicalDomainId == clinicalDomain.OrganizationClinicalDomainId);
                if (!remainingTelecom)
                {
                    clinicalDomain.Delete();
                }
            }
        }

        public void AddNewOrUpdateOldEntries(List<OrganizationClinicalDomain> upcomingEntries)
        {
            foreach (var organizationClinidalDomain in upcomingEntries)
            {
                if (organizationClinidalDomain.OrganizationClinicalDomainId == 0)
                {
                    ClinicalDomains.Add(organizationClinidalDomain);
                }
            }
        }

        #endregion /Edit Child entries
    }
}
