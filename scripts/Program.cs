using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;

class Program
{
    static IConfigurationRoot Configuration { get; set; }
    static string connectionString;
    static void Main()
    {
        LoadConfiguration();

        if (!TryConnectToDatabase())
        {
            Console.WriteLine("La conexión a la base de datos ha fallado. El programa se cerrará.");
            return;
        }

        Console.WriteLine("\nBienvenido al sistema de gestión de pagos.");

        bool usingMachine = true;

        while (usingMachine)
        {
            Console.WriteLine("\n¿Qué deseas hacer?");
            Console.WriteLine("1. Ver facturas");
            Console.WriteLine("2. Añadir factura");
            Console.WriteLine("3. Ver balance de cuentas");
            Console.WriteLine("4. Añadir cuenta");
            Console.WriteLine("5. Pagar facturas");
            Console.WriteLine("6. Salir");
            Console.Write("Elige una opción: ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    ViewInvoices();
                    break;

                case "2":
                    AddInvoice();
                    break;

                case "3":
                    ViewAccountBalances();
                    break;

                case "4":
                    AddAccount();
                    break;

                case "5":
                    PayInvoices();
                    break;

                case "6":
                    Console.WriteLine("Saliendo del programa...");
                    usingMachine = false;
                    break;

                default:
                    Console.WriteLine("Opción no válida. Intenta de nuevo.");
                    break;
            }
        }
    }

    static void LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("json/settings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();

        connectionString = $"Host={Configuration["DatabaseConfig:Host"]};" +
                           $"Port={Configuration["DatabaseConfig:Port"]};" +
                           $"Username={Configuration["DatabaseConfig:Username"]};" +
                           $"Password={Configuration["DatabaseConfig:Password"]};" +
                           $"Database={Configuration["DatabaseConfig:Database"]};";
    }

    static bool TryConnectToDatabase()
    {
        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error de conexión: {ex.Message}");
            return false;
        }
    }

    static void ViewInvoices()
    {
        Console.WriteLine("\nFacturas pendientes:");
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT paymentid, paymentdescription, paymentamount, paymentdue FROM payments WHERE paymentcompleted = FALSE";
            using (var command = new NpgsqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                int index = 1;
                while (reader.Read())
                {
                    Console.WriteLine($"{index}. {reader["paymentdescription"]} - Monto: {reader["paymentamount"]} - Fecha de vencimiento: {reader["paymentdue"]}");
                    index++;
                }
            }
        }
    }

    static void AddInvoice()
    {
        Console.WriteLine("\nAñadir nueva factura:");

        Console.Write("Descripción de la factura: ");
        string description = Console.ReadLine();

        Console.Write("Monto de la factura: ");
        decimal amount = decimal.Parse(Console.ReadLine());

        Console.Write("Fecha de vencimiento (yyyy-mm-dd): ");
        DateTime dueDate = DateTime.Parse(Console.ReadLine());

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO payments (paymentdescription, paymentamount, paymentdue) VALUES (@description, @amount, @dueDate)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("description", description);
                command.Parameters.AddWithValue("amount", amount);
                command.Parameters.AddWithValue("dueDate", dueDate);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Factura añadida correctamente.");
    }

    static void ViewAccountBalances()
    {
        Console.WriteLine("\nBalance de cuentas:");

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT accountname, accountbalance FROM bankaccount";
            using (var command = new NpgsqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"{reader["accountname"]}: {reader["accountbalance"]}");
                }
            }
        }
    }

    static void AddAccount()
    {
        Console.WriteLine("\nAñadir nueva cuenta:");

        Console.Write("Nombre de la cuenta: ");
        string accountName = Console.ReadLine();

        Console.Write("Balance de la cuenta: ");
        decimal accountBalance = decimal.Parse(Console.ReadLine());

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO bankaccount (accountname, accountbalance) VALUES (@accountName, @accountBalance)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("accountName", accountName);
                command.Parameters.AddWithValue("accountBalance", accountBalance);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Cuenta añadida correctamente.");
    }

    static void PayInvoices()
    {
        Console.WriteLine("\nSelecciona las facturas que deseas pagar:");

        List<int> unpaidInvoiceIds = new List<int>();
        decimal totalAmountToPay = 0;

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT paymentid, paymentdescription, paymentamount FROM payments WHERE paymentcompleted = FALSE";
            using (var command = new NpgsqlCommand(query, connection))
            using (var reader = command.ExecuteReader())
            {
                int index = 1;
                while (reader.Read())
                {
                    Console.WriteLine($"{index}. {reader["paymentdescription"]} - Monto: {reader["paymentamount"]}");
                    unpaidInvoiceIds.Add((int)reader["paymentid"]);
                    totalAmountToPay += (decimal)reader["paymentamount"];
                    index++;
                }
            }
        }

        Console.Write("Ingresa el número de la factura a pagar o 0 para cancelar: ");
        int invoiceToPay = int.Parse(Console.ReadLine());

        while (invoiceToPay != 0)
        {
            if (invoiceToPay > 0 && invoiceToPay <= unpaidInvoiceIds.Count)
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string updateQuery = "UPDATE payments SET paymentcompleted = TRUE WHERE paymentid = @paymentId";
                    using (var command = new NpgsqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("paymentId", unpaidInvoiceIds[invoiceToPay - 1]);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine($"Factura {invoiceToPay} pagada.");
            }
            else
            {
                Console.WriteLine("Número de factura no válido.");
            }

            Console.Write("Ingresa el número de la factura a pagar o 0 para cancelar: ");
            invoiceToPay = int.Parse(Console.ReadLine());
        }

        Console.WriteLine("\nSelecciona la cuenta para pagar:");
        ViewAccountBalances();

        Console.Write("Ingresa el nombre de la cuenta: ");
        string accountToUse = Console.ReadLine();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT accountbalance FROM bankaccount WHERE accountname = @accountName";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("accountName", accountToUse);
                decimal accountBalance = (decimal)command.ExecuteScalar();

                if (accountBalance >= totalAmountToPay)
                {
                    decimal newBalance = accountBalance - totalAmountToPay;

                    string updateBalanceQuery = "UPDATE bankaccount SET accountbalance = @newBalance WHERE accountname = @accountName";
                    using (var updateCommand = new NpgsqlCommand(updateBalanceQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("newBalance", newBalance);
                        updateCommand.Parameters.AddWithValue("accountName", accountToUse);
                        updateCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine($"Pago realizado con éxito. Nuevo balance de la cuenta '{accountToUse}': {newBalance}");
                }
                else
                {
                    Console.WriteLine("Saldo insuficiente en la cuenta para realizar el pago.");
                }
            }
        }
    }
}
