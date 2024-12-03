﻿using sReportsV2.Common.Enums;
using sReportsV2.Domain.Sql.Entities.Common;
using sReportsV2.Domain.Sql.Entities.PatientList;
using sReportsV2.Domain.Sql.Entities.ProjectEntry;
using sReportsV2.Domain.Sql.Entities.User;
using sReportsV2.SqlDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sReportsV2.DAL.Sql.Interfaces
{
    public interface IPersonnelDAL : IChildEntryDAL<PersonnelIdentifier>, IChildEntryDAL<PersonnelAddress>
    {
        int CountAll();
        bool IsValidUser(string username, string password);
        Personnel GetById(int id);
        Personnel GetByUsername(string username);
        Personnel GetByEmail(string email);
        void InsertOrUpdate(Personnel user);
        bool IsUserStillValid(int id, int? activeStateCD);
        void Save();
        List<Personnel> GetAllByIds(List<int> ids);
        Task<List<Personnel>> GetAllByIdsAsync(List<int> ids);
        long GetAllCount();
        List<Personnel> GetAllByOrganizationIds(List<int> organizationIds);
        bool IsEmailValid(string email);
        bool IsUsernameValid(string username);
        bool UserExist(int id);
        void SetState(int id, int? state, int organizationId, int? archivedUserStateCD);
        void Delete(int id);
        void UpdateOrganizationsUserCounts(Personnel user, Personnel dbUser);
        void UpdatePassword(Personnel user, string newPassword);
        List<Personnel> GetUsersForCommentTag(string searchWord);
        void UpdateUsersCountForAllOrganization(int? archivedUserStateCD);
        IQueryable<AutoCompleteUserData> GetUsersFilteredByName(string searchValue, int organizationId, int? archivedUserStateCD);
        List<PersonnelView> GetAll(PersonnelFilter userFilter, int? archivedUserStateCD);
        long GetAllFilteredCount(PersonnelFilter userFilter, int? archivedUserStateCD);
        bool ExistIdentifier(PersonnelIdentifier identifier);
        bool UserHasPermission(int userId, string moduleName, string permissionName);
        List<PersonnelPositionPermissionView> GetPermissions(int userId);
        string GetNameInfoById(int id);
        Task<IDictionary<int, string>> GetUsersDictionaryAsync(IEnumerable<int> userIds);
        int? GetDoctorId(List<PersonnelIdentifier> personnelIdentifiers);
        IQueryable<Personnel> FilterForAutocomplete(PersonnelAutocompleteFilter personnelAutocompleteFilter, int? archivedUserStateCD);
        void InsertOrUpdatePersonnelOccupation(Personnel user, PersonnelOccupation personnelOccupation);
        Task<PaginationData<Personnel>> FilterAndCountPersonnelByProject(ProjectFilter filter, int? archivedUserStateCD);
        Task<PaginationData<Personnel>> GetAllPatientListPersonnelsAndCount(PatientListFilter filter, int? archivedUserStateCD);
    }
}
