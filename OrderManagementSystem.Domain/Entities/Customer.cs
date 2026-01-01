using OrderManagementSystem.Domain.Common;
using OrderManagementSystem.Domain.Exceptions;

namespace OrderManagementSystem.Domain.Entities
{
    public class Customer : BaseEntity
    {
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Phone { get; private set; } = string.Empty;
        public string Address { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }

        private Customer() { }

        public Customer(string firstName, string lastName, string email, string phone = "", string address = "", string createdBy = "")
        {
            SetFirstName(firstName);
            SetLastName(lastName);
            SetEmail(email);
            SetPhone(phone);
            SetAddress(address);
            IsActive = true;

            if (!string.IsNullOrEmpty(createdBy))
                CreatedBy = createdBy;
        }

        public void UpdateContactInfo(string phone, string address)
        {
            SetPhone(phone);
            SetAddress(address);
            UpdateTimestamps();
        }

        public void UpdateName(string firstName, string lastName)
        {
            SetFirstName(firstName);
            SetLastName(lastName);
            UpdateTimestamps();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamps();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamps();
        }

        private void SetFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("First name cannot be empty");

            FirstName = firstName.Trim();
        }

        private void SetLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Last name cannot be empty");

            LastName = lastName.Trim();
        }

        private void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("Email cannot be empty");

            if (!IsValidEmail(email))
                throw new DomainException("Invalid email format");

            Email = email.Trim().ToLower();
        }

        private void SetPhone(string phone)
        {
            Phone = phone?.Trim() ?? string.Empty;
        }

        private void SetAddress(string address)
        {
            Address = address?.Trim() ?? string.Empty;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}