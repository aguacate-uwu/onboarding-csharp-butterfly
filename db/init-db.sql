CREATE TABLE IF NOT EXISTS payments (
    paymentid SERIAL PRIMARY KEY,
    paymentdescription TEXT DEFAULT 'No description was provided for this payment.',
    paymentamount DECIMAL(10,2) NOT NULL,
    paymentcompleted BOOLEAN DEFAULT FALSE,
    paymentdue DATE NOT NULL
);

CREATE TABLE IF NOT EXISTS bankaccount (
    accountid SERIAL PRIMARY KEY,
    accountname VARCHAR(20) UNIQUE NOT NULL,
    accountbalance DECIMAL(10,2) NOT NULL DEFAULT 0
);

INSERT INTO payments (paymentdescription, paymentamount, paymentcompleted, paymentdue) VALUES
    ('Rent', 500.00, TRUE, '2022-06-30'),
    ('Groceries', 150.00, FALSE, '2022-07-15'),
    ('Gas', 30.00, FALSE, '2022-07-20'),
    ('Phone bill', 80.00, TRUE, '2022-07-25'),
    ('Electricity', 100.00, FALSE, '2022-07-30'),
    ('Car payment', 120.00, FALSE, '2022-08-05');

-- Insertar datos en bankaccount
INSERT INTO bankaccount (accountname, accountbalance) VALUES
    ('Checking', 2000.00),
    ('Savings', 6000.00);
