using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MultipartDataMediaFormatter.Infrastructure;

namespace MultipartDataMediaFormatter.Tests.Models
{
    public class PersonModel
    {
        public Guid PersonId { get; set; }

        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public DateTime? RegisteredDateTime { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public int? Age { get; set; }

        public decimal? Score { get; set; }

        public double? ScoreScaleFactor { get; set; }

        public float? ActivityProgress { get; set; }

        public bool IsActive { get; set; }

        public PersonTypes? PersonType { get; set; }

        [Required]
        public HttpFile Photo { get; set; }

        public Dictionary<string, PersonProperty> Properties { get; set; }

        public List<PersonRole> Roles { get; set; }

        public List<HttpFile> Attachments { get; set; }

        public SomeValue<PersonProperty> SomeGenericProperty { get; set; }
    }

    public class PersonProperty
    {
        public int PropertyCode { get; set; }
        public string PropertyName { get; set; }
    }

    public class PersonRole
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }

    public class SomeValue<T>
    {
        public string Name { get; set; }
        [Required]
        public T GenericValue { get; set; }
    }
    
    public enum PersonTypes
    {
        Admin,
        User
    }
}
