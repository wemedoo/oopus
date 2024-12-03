﻿using AutoMapper;
using sReportsV2.BusinessLayer.Interfaces;
using sReportsV2.Common.Enums.DocumentPropertiesEnums;
using sReportsV2.Common.Extensions;
using sReportsV2.DAL.Sql.Interfaces;
using sReportsV2.Domain.Entities.Common;
using sReportsV2.Domain.Sql.Entities.CodeEntry;
using sReportsV2.Domain.Sql.Entities.Common;
using sReportsV2.DTOs.Common;
using sReportsV2.DTOs.CodeEntry.DataOut;
using sReportsV2.DTOs.CodeEntry.DataIn;
using sReportsV2.DTOs.Pagination;
using sReportsV2.SqlDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using sReportsV2.Domain.Sql.Entities.TaskEntry;
using System.Configuration;
using System.Data.SqlClient;
using sReportsV2.Common.Constants;
using sReportsV2.Domain.Sql.Entities.ThesaurusEntry;
using sReportsV2.SqlDomain.Implementations;
using sReportsV2.DTOs.ThesaurusEntry;
using sReportsV2.Cache.Singleton;
using sReportsV2.Common.Enums;
using Microsoft.Extensions.Configuration;

namespace sReportsV2.BusinessLayer.Implementations
{
    public class CodeBLL : ICodeBLL
    {
        private readonly ICodeDAL codeDAL;
        private readonly ICodeSetDAL codeSetDAL;
        private readonly ICodeAssociationDAL codeAssociationDAL;
        private readonly ITaskDAL taskDAL;
        private readonly IThesaurusDAL thesaurusDAL;
        private readonly IMapper Mapper;
        private readonly IConfiguration configuration;

        public CodeBLL(ICodeDAL codeDAL, ICodeSetDAL codeSetDAL, ICodeAssociationDAL codeAssociationDAL, ITaskDAL taskDAL, IThesaurusDAL thesaurusDAL, IMapper mapper, IConfiguration configuration)
        {
            this.codeDAL = codeDAL;
            this.codeSetDAL = codeSetDAL; 
            this.codeAssociationDAL = codeAssociationDAL;
            this.taskDAL = taskDAL;
            this.thesaurusDAL = thesaurusDAL;
            Mapper = mapper;
            this.configuration = configuration;
        }

        public void Delete(int codeId)
        {
            codeDAL.Delete(codeId);
        }

        public int Insert(CodeDataIn codeDataIn)
        {
            codeDataIn = Ensure.IsNotNull(codeDataIn, nameof(codeDataIn));
            Code entry = Mapper.Map<Code>(codeDataIn);
            int codeId = codeDAL.Insert(entry);
            UpdateAssociations(codeId, entry.ActiveTo);

            return codeId;
        }

        public Dictionary<string, List<EnumData>> GetDocumentPropertiesEnums()
        {
            Dictionary<string, List<EnumData>> result = new Dictionary<string, List<EnumData>>();
            result["Classes"] = GetDocumentClassesList();
            result["GeneralPurpose"] = GetDocumentGeneralPurposesList();
            result["ExplicitPurpose"] = GetDocumentExplicitPurposesList();
            result["ContextDependent"] = GetDocumentContextDependentsList();
            result["ScopeOfValidity"] = GetDocumentScopeOfValidityList();
            result["ClinicalDomain"] = GetDocumentClinicalDomainList();
            result["ClinicalContext"] = GetDocumentClinicalContextList();
            result["AdministrativeContext"] = GetDocumentAdministrativeContextList();
            result["FollowUp"] = GetDocumentFollowUpList();
            return result;
        }

        private List<EnumData> GetDocumentClassesList()
        {
            return Enum.GetValues(typeof(DocumentClassEnum)).Cast<DocumentClassEnum>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentGeneralPurposesList()
        {
            return Enum.GetValues(typeof(DocumentGeneralPurposeEnum)).Cast<DocumentGeneralPurposeEnum>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentExplicitPurposesList()
        {
            return Enum.GetValues(typeof(DocumentExplicitPurpose)).Cast<DocumentExplicitPurpose>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentContextDependentsList()
        {
            return Enum.GetValues(typeof(ContextDependent)).Cast<ContextDependent>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentScopeOfValidityList()
        {
            return Enum.GetValues(typeof(DocumentScopeOfValidityEnum)).Cast<DocumentScopeOfValidityEnum>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentClinicalDomainList()
        {
            return SingletonDataContainer.Instance.GetCodesByCodeSetId((int)CodeSetList.ClinicalDomain)
                .Select(x =>
                new EnumData
                {
                    Value = x.Thesaurus.GetPreferredTermByTranslationOrDefault("en"),
                    Label = x.Thesaurus.GetPreferredTermByTranslationOrDefault("en")
                })
                .ToList();
        }

        private List<EnumData> GetDocumentClinicalContextList()
        {
            return Enum.GetValues(typeof(DocumentClinicalContextEnum)).Cast<DocumentClinicalContextEnum>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentAdministrativeContextList()
        {
            return Enum.GetValues(typeof(AdministrativeContext)).Cast<AdministrativeContext>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        private List<EnumData> GetDocumentFollowUpList()
        {
            return Enum.GetValues(typeof(FollowUp)).Cast<FollowUp>()
                .Select(x =>
                new EnumData
                {
                    Value = x.ToString(),
                    Label = x.ToString()
                })
                .ToList();
        }

        public PaginationDataOut<CodeDataOut, DataIn> GetAllFiltered(CodeFilterDataIn dataIn)
        {
            CodeFilter filter = Mapper.Map<CodeFilter>(dataIn);
            PaginationDataOut<CodeDataOut, DataIn> result = new PaginationDataOut<CodeDataOut, DataIn>()
            {
                Count = (int)this.codeDAL.GetAllEntriesCount(filter),
                Data = Mapper.Map<List<CodeDataOut>>(this.codeDAL.GetAll(filter)),
                DataIn = dataIn
            };

            return result;
        }

        public PaginationDataOut<CodeDataOut, DataIn> GetAllAssociationsFiltered(CodeFilterDataIn dataIn)
        {
            CodeFilter filter = Mapper.Map<CodeFilter>(dataIn);
            if (!string.IsNullOrEmpty(filter.CodeSetDisplay) && filter.CodeSetId == 0)
                filter.CodeSetId = codeSetDAL.GetIdByPreferredTerm(filter.CodeSetDisplay);
            
            PaginationDataOut<CodeDataOut, DataIn> result = new PaginationDataOut<CodeDataOut, DataIn>()
            {
                Count = (int)this.codeDAL.GetAllAssociationsCount(filter),
                Data = Mapper.Map<List<CodeDataOut>>(this.codeDAL.GetAllAssociationsFiltered(filter)),
                DataIn = dataIn
            };

            return result;
        }

        public List<CodeDataOut> GetByCodeSetId(int codeSetId)
        {
            List<CodeDataOut> codesDataOut = Mapper.Map<List<CodeDataOut>>(codeDAL.GetByCodeSetId(codeSetId));
            return codesDataOut;
        }

        public Code GetById(int codeId)
        {
            return codeDAL.GetById(codeId);
        }

        public int GetIdByPreferredTerm(string preferredTerm)
        {
            return codeDAL.GetIdByPreferredTerm(preferredTerm);
        }

        public int GetByCodeSetIdAndPreferredTerm(int codeSetId, string preferredTerm)
        {
            return codeDAL.GetByCodeSetIdAndPreferredTerm(codeSetId, preferredTerm);
        }

        private void UpdateAssociations(int codeId, DateTimeOffset activeTo)
        {
            var codeAssociations = codeAssociationDAL.GetAllByParentOrChildId(codeId);
            if (codeAssociations != null)
            {
                codeAssociationDAL.Insert(codeAssociations, activeTo);
            }
        }

        public List<CodeDataOut> GetAssociatedDocuments(int completeQuestionnaireId)
        {
            List<CodeAssociation> associations = codeAssociationDAL.GetAllByParentId(completeQuestionnaireId);
            List<CodeDataOut> documents = new List<CodeDataOut>();
            List<TaskDocument> taskDocuments = taskDAL.GetAllTaskDocuments();
            foreach (var association in associations)
            {
                var codeDataOut = GetDataOutById(association.ChildId);
                if (codeDataOut != null && taskDocuments.Where(x => x.TaskDocumentCD == association.ChildId).Any())
                    documents.Add(codeDataOut);
            }

            return documents;
        }

        public List<CodeDataOut> GetAssociatedCodes(List<int> associationChildIds)
        {
            List<CodeDataOut> codeDataOuts = new List<CodeDataOut>();
            foreach (var codeId in associationChildIds)
            {
                var code = Mapper.Map<CodeDataOut>(codeDAL.GetById(codeId));
                if (code != null)
                    codeDataOuts.Add(code);
            }

            return codeDataOuts;
        }

        private CodeDataOut GetDataOutById(int? codeId)
        {
            CodeDataOut codeDataOut = Mapper.Map<CodeDataOut>(codeDAL.GetById(codeId ?? 0));
            return codeDataOut;
        }

        private int UpdateCode(Code code)
        {
            string connectionString = configuration["Sql"];
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    int codeId = codeDAL.Update(code);
                    UpdateAssociations(codeId, code.ActiveTo);

                    transaction.Commit();
                    return codeId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public int InsertWithPreferredTerm(CodeDataIn codeDataIn, string preferredTerm)
        {
            int thesaurusId = InsertWithPreferredTerm(preferredTerm);

            Code code = Mapper.Map<Code>(codeDataIn);
            code.ThesaurusEntryId = thesaurusId;
            codeDAL.Insert(code);

            return code.CodeId;
        }

        private int InsertWithPreferredTerm(string preferredTerm)
        {
            int thesaurusId;
            ThesaurusEntry thesaurusEntryDb = thesaurusDAL.GetByPreferredTerm(preferredTerm);

            if (thesaurusEntryDb != null)
                thesaurusId = thesaurusEntryDb.ThesaurusEntryId;
            else
            {
                ThesaurusEntry thesaurus = Mapper.Map<ThesaurusEntry>(new ThesaurusEntryDataIn()
                {
                    Translations = new List<ThesaurusEntryTranslationDataIn>()
                    {
                        new ThesaurusEntryTranslationDataIn
                        {
                            Language = LanguageConstants.EN,
                            PreferredTerm = preferredTerm,
                            Definition = preferredTerm
                        }
                    }
                });
                thesaurusDAL.InsertOrUpdate(thesaurus);
                thesaurusId = thesaurus.ThesaurusEntryId;
            }

            return thesaurusId;
        }
    }
}