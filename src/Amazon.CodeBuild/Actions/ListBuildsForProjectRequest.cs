﻿#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Amazon.CodeBuild
{
    public class ListBuildsForProjectRequest : ICodeBuildRequest
    {
        public ListBuildsForProjectRequest(
            string projectName, 
            string? sortOrder = null,
            string? nextToken = null)
        {
            ProjectName = projectName;
            SortOrder = sortOrder;
            NextToken = nextToken;
        }

        [Required]
        public string ProjectName { get; }

        // ASCENDING | DESCENDING
        public string? SortOrder { get; }

        public string? NextToken { get; }
    }
}