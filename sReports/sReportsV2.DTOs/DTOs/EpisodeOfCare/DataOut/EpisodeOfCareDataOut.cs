﻿using sReportsV2.DTOs.Common.DTO;
using sReportsV2.DTOs.DigitalGuidelineInstance.DataOut;
using sReportsV2.DTOs.CodeEntry.DataOut;
using sReportsV2.DTOs.Encounter;
using sReportsV2.DTOs.Patient;
using System;
using System.Collections.Generic;
using sReportsV2.DTOs.DTOs.PersonnelTeam.DataOut;
using System.Linq;

namespace sReportsV2.DTOs.EpisodeOfCare
{
    public class EpisodeOfCareDataOut
    {
        public string Description { get; set; }
        public List<GuidelineInstanceDataOut> ListGuidelines { get; set; } = new List<GuidelineInstanceDataOut>();
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string OrganizationRef { get; set; }
        public int Status { get; set; }
        public int Type { get; set; }
        public string DiagnosisCondition { get; set; }
        public string DiagnosisRank { get; set; }
        public PeriodDTO Period { get; set; }
        public List<DiagnosticReportDataOut> DiagnosticReports { get; set; }
        public PatientDataOut Patient { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
        public List<EncounterDataOut> Encounters;
        public PersonnelTeamDataOut PersonnelTeam { get; set; }
        public int NumOfDocuments { get; set; }
        public int NumOfEncounters { get; set; }

        public string ConvertTypeCDToDisplayName(List<CodeDataOut> episodeOfCaresTypes, string language)
        {
            return episodeOfCaresTypes.Where(x => x.Id == this.Type).FirstOrDefault()?.Thesaurus?.GetPreferredTermByTranslationOrDefault(language) ?? String.Empty;
        }

        public string ConvertEOCAndEncounterTypeCDToDisplayName(List<CodeDataOut> episodeOfCaresTypes, List<CodeDataOut> encounterTypes, string language, int? encounterId = null)
        {
            int eocTypeCD = this.Type;
            var encounter = GetEncounter(encounterId);
            string encounterTypeName = String.Empty;
            if (encounter != null)
            {
                encounterTypeName = " - " + encounterTypes.Where(x => x.Id == encounter.TypeId).FirstOrDefault()?.Thesaurus?.GetPreferredTermByTranslationOrDefault(language);
            }
            return episodeOfCaresTypes.Where(x => x.Id == eocTypeCD).FirstOrDefault()?.Thesaurus?.GetPreferredTermByTranslationOrDefault(language) + encounterTypeName;
        }

        private EncounterDataOut GetEncounter(int? encounterId)
        {
            if (encounterId == 0 || encounterId == null)
                return this.Encounters.OrderByDescending(x => x.EntryDatetime).FirstOrDefault();
            else
                return this.Encounters.FirstOrDefault(x => x.Id == encounterId);
        }
    }
}