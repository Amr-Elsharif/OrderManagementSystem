using OrderManagementSystem.Domain.Exceptions;

namespace OrderManagementSystem.Domain.ValueObjects
{
    public class Money
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        // Parameterless constructor for EF Core
        private Money()
        {
            Currency = "USD";
        }

        public Money(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new DomainException("Money amount cannot be negative");

            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Currency cannot be empty");

            Amount = amount;
            Currency = currency.ToUpper();
        }

        public static Money Zero(string currency = "USD") => new(0, currency);

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException("Cannot add money with different currencies");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new DomainException("Cannot subtract money with different currencies");

            if (other.Amount > Amount)
                throw new DomainException("Insufficient funds");

            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal multiplier)
        {
            if (multiplier < 0)
                throw new DomainException("Multiplier cannot be negative");

            return new Money(Amount * multiplier, Currency);
        }

        public override string ToString() => $"{Amount:N2} {Currency}";
    }
}