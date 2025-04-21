using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Serilog;

class Program
{
    static IConfigurationRoot Configuration { get; set; }
    static string connectionString;
    static int? usuarioIdActual = null;
    static string usuarioActual = null;
    static void Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/myapplogs.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Hello, world!");

        LoadConfiguration(); //Carga la configuración

        if (!TryConnectToDatabase()) //Intentamos conectarnos a la base de datos
        {
            Console.WriteLine("La conexión a la base de datos ha fallado. El programa se cerrará.");
            return;
        }

        Console.WriteLine("\nBienvenido al sistema de gestión de pagos."); //Da la bienvenida indicando que el programa está funcionando correctamente

        bool loginUser = false;
        bool usingMachine = false;

        while (!loginUser)
        {
            if (LoginUsuario() == true)
            {
                usingMachine = true;
                loginUser = true;
                break;
            }
            Console.WriteLine("\n¿Qué deseas hacer?");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Salir");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    RegistroUsuario(); 
                    break;

                case "2":
                    break;

                 case "3":
                    Console.WriteLine("Saliendo del programa...");
                    loginUser = true;
                    break;

                default:
                    Console.WriteLine("Opción no válida. Intenta de nuevo.");
                    break;
            }
            
        }

        static void RegistroUsuario()
        {
            Console.WriteLine("Por favor, elige un nombre de usuario:");
            string newUsername = Console.ReadLine();
            Console.WriteLine("Por favor, elige una contraseña:");
            string newPassword = Console.ReadLine();
            Console.WriteLine("Por favor, repite la contraseña:");
            string confirmPassword = Console.ReadLine();
            if (newPassword != confirmPassword)
            {
                Console.WriteLine("Las contraseñas no coinciden. Inténtalo de nuevo.");
                return;
            }
            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                Console.WriteLine("El nombre de usuario y la contraseña no pueden estar vacíos.");
                return;
            }
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM usuarios WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", newUsername);
                    long count = (long)command.ExecuteScalar();

                    if (count > 0)
                    {
                        Console.WriteLine("Este nombre de usuario ya existe.");
                        return;
                    }
                }
            }

            static string EncriptarContraseña(string contraseña)
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(contraseña);
                    byte[] hashBytes = sha256Hash.ComputeHash(bytes);
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        builder.Append(hashBytes[i].ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
            string passwordEncriptada = EncriptarContraseña(newPassword);
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO usuarios (username, password_hash) VALUES (@username, @password_hash)";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", newUsername);
                    command.Parameters.AddWithValue("@password_hash", passwordEncriptada);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Usuario registrado correctamente.");
                }
            }
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT id FROM usuarios WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", newUsername);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {   
                            int usuarioId = reader.GetInt32(0);
                            usuarioIdActual = usuarioId;
                        }
                    }
                }
            }
            
            AddAccount();

            Log.Information("Usuario '{newUsername}' registrado y cuanta del banco añadida.", newUsername);

        }

        static bool LoginUsuario()
        {
            static string EncriptarContraseña(string contraseña)
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(contraseña);
                    byte[] hashBytes = sha256Hash.ComputeHash(bytes);
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        builder.Append(hashBytes[i].ToString("x2"));
                    }
                    return builder.ToString();
                }
            }
            Console.WriteLine("Introduce tu nombre de usuario:");
            string username = Console.ReadLine();

            Console.WriteLine("Introduce tu contraseña:");
            string password = Console.ReadLine();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT id, password_hash FROM usuarios WHERE username = @username";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {   
                            int idUsuario = reader.GetInt32(0);
                            string passwordHashGuardada = reader.GetString(1);
                            string passwordEncriptada = EncriptarContraseña(password);

                            if (passwordEncriptada == passwordHashGuardada)
                            {
                                Console.WriteLine("Has iniciado sesión correctamente.");
                                usuarioIdActual = idUsuario; 
                                usuarioActual = username;
                                return true;
                            }
                            else
                            {
                                Console.WriteLine("Contraseña incorrecta. Inténtalo de nuevo.");
                                return false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("No se encontró ningún usuario con ese nombre.");
                            return false;
                        }
                    }
                }
            }
        }

        while (usingMachine) //Este bucle sirve para leer los inputs del usuario y elegir una de las funciones disponibles
        {
            Console.WriteLine("\n¿Qué deseas hacer?");
            Console.WriteLine("1. Ver facturas");
            Console.WriteLine("2. Añadir factura");
            Console.WriteLine("3. Ver balance de cuentas");
            Console.WriteLine("4. Añadir cuenta");
            Console.WriteLine("5. Pagar facturas");
            Console.WriteLine("6. Eliminar facturas");
            Console.WriteLine("7. Eliminar cuenta");
            Console.WriteLine("8. Añadir balance");
            Console.WriteLine("9. Salir");
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
                    DeleteInvoices();
                    break;

                case "7":
                    DeleteAccount();
                    break;

                case "8":
                    AddBalance();
                    break;

                case "9":
                    Console.WriteLine("Saliendo del programa...");
                    usingMachine = false; //Al introducir el 7 el bucle se termina
                    break;

                default:
                    Console.WriteLine("Opción no válida. Intenta de nuevo."); //Esto sirve para cuando el input introducido por el usuario no es válido
                    break;
            }
        }
    }

    static void LoadConfiguration() //Esto ajusta la configuración conectándose a la base de datos
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

    static bool TryConnectToDatabase() //Esta función defina la forma en la que intentamos conectarnos a la base de datos
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

    static void ViewInvoices() //Esta función muestra las facturas seleccionándolas de la base de datos
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nFacturas pendientes:");
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT paymentid, paymentdescription, paymentamount, paymentdue FROM payments WHERE paymentcompleted = FALSE AND user_id = @usuarioId";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@usuarioId", usuarioId);
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
    }

    static void AddInvoice() //Esta función añade una nueva factura a la base de datos
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nAñadir nueva factura:");

        Console.Write("Descripción de la factura: ");
        string description = Console.ReadLine();

        Console.Write("Monto de la factura: ");
        string inputAmount = Console.ReadLine();
        decimal amount;
        while (string.IsNullOrEmpty(inputAmount) || !decimal.TryParse(inputAmount, out amount))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir el monto de la factura otra vez: ");
            inputAmount = Console.ReadLine();
        }

        Console.Write("Fecha de vencimiento (yyyy-mm-dd): ");
        string inputDueDate = Console.ReadLine();
        DateTime dueDate;
        while (string.IsNullOrEmpty(inputDueDate) || !DateTime.TryParse(inputDueDate, out dueDate))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir la fecha de vencimiento otra vez: ");
            inputDueDate = Console.ReadLine();
        }

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO payments (paymentdescription, paymentamount, paymentdue, user_id) VALUES (@description, @amount, @dueDate, @usuarioId)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("description", description);
                command.Parameters.AddWithValue("amount", amount);
                command.Parameters.AddWithValue("dueDate", dueDate);
                command.Parameters.AddWithValue("usuarioId", usuarioId);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Factura añadida correctamente."); //Confirmación de que la nueva factura ha sido añadida
    }

    static void ViewAccountBalances() //Esta función te muestra el balance de cuentas
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nBalance de cuentas:");

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT accountname, accountbalance FROM bankaccount WHERE user_id = @usuarioId";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@usuarioId", usuarioId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"{reader["accountname"]}: {reader["accountbalance"]}");
                    }
                }
            }
        }
    }

    static void AddAccount() //Esta función te permite añadir una cuenta nueva a la base de datos
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nAñadir nueva cuenta:");
        
        Console.Write("Nombre de la cuenta. ");
        bool nameValidate = false;
        string accountName = "";
        while (string.IsNullOrEmpty(accountName) || (nameValidate == false))
        {
            Console.WriteLine("Debes introducir un nombre válido:");
            accountName = Console.ReadLine();

            if (!string.IsNullOrEmpty(accountName))
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {                      
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM bankaccount WHERE accountname = @accountName";
                    using (var command = new NpgsqlCommand(query, connection))
                    {   
                        command.Parameters.AddWithValue("@accountName", accountName);
                        long nombreRepetido = (long)command.ExecuteScalar();
                        if (nombreRepetido > 0)
                        {
                            Console.WriteLine("Ese nombre de cuenta ya existe.");
                        }
                        else
                        {
                            nameValidate = true;
                        }
                    }
                }
            }
        }

        Console.Write("Balance de la cuenta: ");
        string inputAccountBalance = Console.ReadLine();
        decimal accountBalance;
        while (string.IsNullOrEmpty(inputAccountBalance) || !decimal.TryParse(inputAccountBalance, out accountBalance))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir el balance de la cuenta otra vez: ");
            inputAccountBalance = Console.ReadLine();
        }

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO bankaccount (accountname, accountbalance, user_id) VALUES (@accountName, @accountBalance, @usuarioId)";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("accountName", accountName);
                command.Parameters.AddWithValue("accountBalance", accountBalance);
                command.Parameters.AddWithValue("@usuarioId", usuarioId);
                command.ExecuteNonQuery();
            }
        }

        Console.WriteLine("Cuenta añadida correctamente."); //Confirmación de que la nueva cuenta ha sido añadida
    }

    static void PayInvoices() //Esta función sirve para pagar las facuras pendientes
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nSelecciona las facturas que deseas pagar:"); //Primero seleccionas las facturas

        List<int> unpaidInvoiceIds = new List<int>();
        List<int> selectedInvoiceIdsToPay = new List<int>();
        decimal totalAmountToPay = 0;
        Dictionary<int, decimal> invoiceAmounts = new Dictionary<int, decimal>();

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT paymentid, paymentdescription, paymentamount FROM payments WHERE paymentcompleted = FALSE AND user_id = @usuarioId";
            using (var command = new NpgsqlCommand(query, connection))   
            {
                command.Parameters.AddWithValue("@usuarioId", usuarioId);
                using (var reader = command.ExecuteReader())
                {
                    int index = 1;
                    while (reader.Read())
                    {
                        int paymentId = (int)reader["paymentid"];
                        decimal paymentAmount = (decimal)reader["paymentamount"];
                        Console.WriteLine($"{index}. {reader["paymentdescription"]} - Monto: {paymentAmount}");
                        unpaidInvoiceIds.Add(paymentId);
                        invoiceAmounts.Add(index, paymentAmount); //Guardamos el índice con el ID y el monto
                        index++;
                    }
                }
            }
        }

        Console.Write("Ingresa el número de la factura a pagar o 0 para cancelar: "); //Aquí confirmas cuáles vas a pagar con un bucle
        string inputInvoiceToPay = Console.ReadLine();
        int invoiceToPay;
        while (string.IsNullOrEmpty(inputInvoiceToPay) || !int.TryParse(inputInvoiceToPay, out invoiceToPay))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir el monto de la factura otra vez: ");
            inputInvoiceToPay = Console.ReadLine();
        }
        while (invoiceToPay != 0)
        {
            if (invoiceToPay > 0 && invoiceToPay <= unpaidInvoiceIds.Count)
            {
                selectedInvoiceIdsToPay.Add(unpaidInvoiceIds[invoiceToPay - 1]);
                totalAmountToPay += invoiceAmounts[invoiceToPay];
                Console.WriteLine($"Factura {invoiceToPay} seleccionada para pagar.");
            }
            else
            {
                Console.WriteLine("Número de factura no válido.");
            }

            Console.Write("Ingresa el número de la factura a pagar o 0 para cancelar: ");
            inputInvoiceToPay = Console.ReadLine();
            while (string.IsNullOrEmpty(inputInvoiceToPay) || !int.TryParse(inputInvoiceToPay, out invoiceToPay))
            {
                Console.WriteLine("No puedes introducir ese valor.");
                Console.Write("Intenta introducir el monto de la factura otra vez: ");
                inputInvoiceToPay = Console.ReadLine();
            }
        }

        Console.WriteLine($"\nTotal a pagar: {totalAmountToPay}");
        Console.WriteLine("\nSelecciona la cuenta para pagar:"); //Seleccionas la cuenta y el nombre de la cuenta
        ViewAccountBalances();

        Console.Write("Ingresa el nombre de la cuenta: ");
        bool nameValidate = false;
        string accountToUse = Console.ReadLine();
        while (string.IsNullOrEmpty(accountToUse) || (nameValidate == false))
        {   
            ViewAccountBalances();
            Console.WriteLine("Debes introducir un nombre válido.");
            accountToUse = Console.ReadLine();

            if (!string.IsNullOrEmpty(accountToUse))
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {                      
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM bankaccount WHERE accountname = @accountName AND user_id = @usuarioId";
                    using (var command = new NpgsqlCommand(query, connection))
                    {   
                        command.Parameters.AddWithValue("@accountName", accountToUse);
                        command.Parameters.AddWithValue("@usuarioId", usuarioId);
                        long nombreRepetido = (long)command.ExecuteScalar();
                        if (nombreRepetido > 0)
                        {   
                            nameValidate = true;
                        }
                        else
                        {
                            Console.WriteLine("Esa cuenta no existe");
                        }
                    }
                }
            }
        }

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
                    foreach (int paymentId in selectedInvoiceIdsToPay)
                    {
                        string updateQuery = "UPDATE payments SET paymentcompleted = TRUE WHERE paymentid = @paymentId";
                        using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("paymentId", paymentId);
                            updateCommand.ExecuteNonQuery();
                        }
                    }

                    decimal newBalance = accountBalance - totalAmountToPay;

                    string updateBalanceQuery = "UPDATE bankaccount SET accountbalance = @newBalance WHERE accountname = @accountName";
                    using (var updateCommand = new NpgsqlCommand(updateBalanceQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("newBalance", newBalance);
                        updateCommand.Parameters.AddWithValue("accountName", accountToUse);
                        updateCommand.ExecuteNonQuery();
                    }


                    Console.WriteLine($"Pago realizado con éxito. Nuevo balance de la cuenta '{accountToUse}': {newBalance}"); //Se confirma el pago y se muestra el balance actualizado 
                }
                else
                {
                    Console.WriteLine("Saldo insuficiente en la cuenta para realizar el pago."); //Se cancela el pago si no hay saldo suficiente
                }
            }
        }
    }
    static void DeleteInvoices()
    {
        int usuarioId = usuarioIdActual.Value;
        Console.WriteLine("\nSelecciona las facturas que deseas eliminar:");

        List<int> unpaidInvoiceIds = new List<int>();
        decimal totalAmountToPay = 0;

        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT paymentid, paymentdescription, paymentamount FROM payments WHERE paymentcompleted = FALSE AND user_id = @usuarioId";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@usuarioId", usuarioId);
                using (var reader = command.ExecuteReader())
                {
                    int index = 1;
                    while (reader.Read())
                    {
                        Console.WriteLine($"{index}. {reader["paymentdescription"]} - Monto: {reader["paymentamount"]}");
                        unpaidInvoiceIds.Add((int)reader["paymentid"]);
                        index++;
                        totalAmountToPay += (decimal)reader["paymentamount"];
                    }
                }
            }
        }

        Console.Write("Ingresa el número de la factura a eliminar o 0 para cancelar: ");
        string inputInvoiceToPay = Console.ReadLine();
        int invoiceToPay;
        while (string.IsNullOrEmpty(inputInvoiceToPay) || !int.TryParse(inputInvoiceToPay, out invoiceToPay))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir el monto de la factura otra vez: ");
            inputInvoiceToPay = Console.ReadLine();
        }
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

                Console.WriteLine($"Factura {invoiceToPay} eliminada.");
            }
            else
            {
                Console.WriteLine("Número de factura no válido.");
            }

            Console.Write("Ingresa el número de la factura a eliminar o 0 para cancelar: ");
            inputInvoiceToPay = Console.ReadLine();
            while (string.IsNullOrEmpty(inputInvoiceToPay) || !int.TryParse(inputInvoiceToPay, out invoiceToPay))
            {
                Console.WriteLine("No puedes introducir ese valor.");
                inputInvoiceToPay = Console.ReadLine();
            }
        }

    }
    static void DeleteAccount()
    {
        int usuarioId = usuarioIdActual.Value;

        bool nameValidate = false;
        string accountToDelete = "";
        Console.WriteLine("Escribe el nombre de la cuenta que deseas eliminar.");
        while (string.IsNullOrEmpty(accountToDelete) || (nameValidate == false))
        {   
            ViewAccountBalances();
            Console.WriteLine("Debes introducir un nombre válido.");
            accountToDelete = Console.ReadLine();

            if (!string.IsNullOrEmpty(accountToDelete))
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {                      
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM bankaccount WHERE accountname = @accountName AND user_id = @usuarioId";
                    using (var command = new NpgsqlCommand(query, connection))
                    {   
                        command.Parameters.AddWithValue("@accountName", accountToDelete);
                        command.Parameters.AddWithValue("@usuarioId", usuarioId);
                        long nombreRepetido = (long)command.ExecuteScalar();
                        if (nombreRepetido > 0)
                        {   
                            nameValidate = true;
                        }
                        else
                        {
                            Console.WriteLine("Esa cuenta no existe.");
                        }
                    }
                }
            }
        }
        
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT accountbalance FROM bankaccount WHERE accountname = @accountName";
            
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@accountName", accountToDelete);
                object result = command.ExecuteScalar();
                decimal accountBalance = (decimal)result;
                if (accountBalance > 0)
                {
                    ViewAccountBalances();
                    Console.WriteLine("Selecciona la cuenta a la que quieres transferir el dinero o la misma cuenta para no hacerlo.");
                    nameValidate = false;
                    string accountToTransfer = "";
                    while (string.IsNullOrEmpty(accountToTransfer) || (nameValidate == false))
                    {   
                        ViewAccountBalances();
                        Console.WriteLine("Debes introducir un nombre válido.");
                        accountToTransfer = Console.ReadLine();

                        if (!string.IsNullOrEmpty(accountToTransfer))
                        {
                            using (var connection2 = new NpgsqlConnection(connectionString))
                            {                        
                                connection2.Open();
                                query = "SELECT COUNT(*) FROM bankaccount WHERE accountname = @accountName AND user_id = @usuarioId";
                                using (var command2 = new NpgsqlCommand(query, connection2))
                                {   
                                    command2.Parameters.AddWithValue("@accountName", accountToTransfer);
                                    command2.Parameters.AddWithValue("@usuarioId", usuarioId);
                                    long nombreRepetido = (long)command2.ExecuteScalar();
                                    if (nombreRepetido > 0)
                                    {   
                                        nameValidate = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Esa cuenta no existe.");
                                    }
                                }
                            }
                        }
                    }
                    if (accountToDelete == accountToTransfer)
                    {
                        query = "DELETE FROM bankaccount WHERE accountname = @accountName";
                        using (var command3 = new NpgsqlCommand(query, connection))
                        {
                            command3.Parameters.AddWithValue("@accountName", accountToDelete);
                            command3.ExecuteNonQuery();
                        }
                        Console.WriteLine("Cuenta eliminada correctamente.");
                        }
                    else
                    {
                        decimal accountBalanceTransfer;
                        query = "SELECT accountbalance FROM bankaccount WHERE accountname = @accountName";
                        using (var command4 = new NpgsqlCommand(query, connection))
                        {
                            command4.Parameters.AddWithValue("accountName", accountToDelete);
                            accountBalance = (decimal)command4.ExecuteScalar();
                        }
                        query = "SELECT accountbalance FROM bankaccount WHERE accountname = @accountName";
                        using (var command5 = new NpgsqlCommand(query, connection))
                        {
                            command5.Parameters.AddWithValue("accountName", accountToTransfer);
                            accountBalanceTransfer = (decimal)command5.ExecuteScalar();
                        }
                        decimal transferBalance = accountBalance + accountBalanceTransfer;
                        string updateBalanceQuery = "UPDATE bankaccount SET accountbalance = @newBalance WHERE accountname = @accountName";
                        using (var updateCommand = new NpgsqlCommand(updateBalanceQuery, connection))
                        {
                            updateCommand.Parameters.AddWithValue("newBalance", transferBalance);
                            updateCommand.Parameters.AddWithValue("accountName", accountToTransfer);
                            updateCommand.ExecuteNonQuery();
                        }
                        Console.WriteLine("Dinero transferido.");
                        query = "DELETE FROM bankaccount WHERE accountname = @accountName";
                        using (var command6 = new NpgsqlCommand(query, connection))
                        {
                            command6.Parameters.AddWithValue("@accountName", accountToDelete);
                            command6.ExecuteNonQuery();
                        }
                        Console.WriteLine("Cuenta eliminada correctamente.");
                    }
                }
                else
                {
                    query = "DELETE FROM bankaccount WHERE accountname = @accountName";
                    using (var command3 = new NpgsqlCommand(query, connection))
                    {
                        command3.Parameters.AddWithValue("@accountName", accountToDelete);
                        command3.ExecuteNonQuery();

                    }
                    Console.WriteLine("Cuenta eliminada correctamente.");
                }
            }
        }
        
    }
    static void AddBalance()
    {
        int usuarioId = usuarioIdActual.Value;
        bool nameValidate = false;
        string accountToUpdate = "";
        Console.WriteLine("Escribe el nombre de la cuenta a la que deseas agregar dinero.");
        while (string.IsNullOrEmpty(accountToUpdate) || (nameValidate == false))
        {   
            ViewAccountBalances();
            Console.WriteLine("Debes introducir un nombre válido.");
            accountToUpdate = Console.ReadLine();

            if (!string.IsNullOrEmpty(accountToUpdate))
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {                      
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM bankaccount WHERE accountname = @accountName AND user_id = @usuarioId";
                    using (var command = new NpgsqlCommand(query, connection))
                    {   
                        command.Parameters.AddWithValue("@accountName", accountToUpdate);
                        command.Parameters.AddWithValue("@usuarioId", usuarioId);
                        long nombreRepetido = (long)command.ExecuteScalar();
                        if (nombreRepetido > 0)
                        {   
                            nameValidate = true;
                        }
                        else
                        {
                            Console.WriteLine("Esa cuenta no existe.");
                        }
                    }
                }
            }
        }
        Console.WriteLine("Introduce la cantidad a añadir.");
        string inputAddBalance = Console.ReadLine();
        decimal addBalance;
        decimal accountBalance;
        while (string.IsNullOrEmpty(inputAddBalance) || !decimal.TryParse(inputAddBalance, out addBalance))
        {
            Console.WriteLine("No puedes introducir ese valor.");
            Console.Write("Intenta introducir la cantidad otra vez: ");
            inputAddBalance = Console.ReadLine();
        }
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT accountbalance FROM bankaccount WHERE accountname = @accountName";
            using (var command = new NpgsqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("accountName", accountToUpdate);
                accountBalance = (decimal)command.ExecuteScalar();
            }
            decimal updatedBalance = accountBalance + addBalance;
            string updateBalanceQuery = "UPDATE bankaccount SET accountbalance = @newBalance WHERE accountname = @accountName";
            using (var updateCommand = new NpgsqlCommand(updateBalanceQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("newBalance", updatedBalance);
                updateCommand.Parameters.AddWithValue("accountName", accountToUpdate);
                updateCommand.ExecuteNonQuery();
            }
            Console.WriteLine($"Dinero transferido. Nuevo balance de la cuenta '{accountToUpdate}': {updatedBalance}");
        }
    }
}
