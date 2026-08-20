-- Base de datos inicial para la primera entrega de Inmobiliaria_JM.
-- Compatible con MySQL y MariaDB (XAMPP).

CREATE DATABASE IF NOT EXISTS inmobiliaria_jm
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE inmobiliaria_jm;

CREATE TABLE IF NOT EXISTS Propietarios (
    IdPropietario INT UNSIGNED NOT NULL AUTO_INCREMENT,
    Dni VARCHAR(8) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Apellido VARCHAR(100) NOT NULL,
    Telefono VARCHAR(30) NULL,
    Email VARCHAR(150) NOT NULL,
    CONSTRAINT PK_Propietarios PRIMARY KEY (IdPropietario),
    CONSTRAINT UQ_Propietarios_Dni UNIQUE (Dni),
    CONSTRAINT UQ_Propietarios_Email UNIQUE (Email),
    CONSTRAINT CK_Propietarios_Dni CHECK (Dni REGEXP '^[0-9]{7,8}$')
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS Inquilinos (
    IdInquilino INT UNSIGNED NOT NULL AUTO_INCREMENT,
    Dni VARCHAR(8) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Apellido VARCHAR(100) NOT NULL,
    Telefono VARCHAR(30) NULL,
    Email VARCHAR(150) NOT NULL,
    CONSTRAINT PK_Inquilinos PRIMARY KEY (IdInquilino),
    CONSTRAINT UQ_Inquilinos_Dni UNIQUE (Dni),
    CONSTRAINT UQ_Inquilinos_Email UNIQUE (Email),
    CONSTRAINT CK_Inquilinos_Dni CHECK (Dni REGEXP '^[0-9]{7,8}$')
) ENGINE = InnoDB;

-- Datos de prueba para comprobar los ABM durante el desarrollo.
INSERT IGNORE INTO Propietarios (Dni, Nombre, Apellido, Telefono, Email)
VALUES
    ('20123456', 'Ana', 'García', '2664123456', 'ana.garcia@example.com'),
    ('22987654', 'Carlos', 'Pérez', '2664987654', 'carlos.perez@example.com');

INSERT IGNORE INTO Inquilinos (Dni, Nombre, Apellido, Telefono, Email)
VALUES
    ('30111222', 'María', 'López', '2664111222', 'maria.lopez@example.com'),
    ('33444555', 'Juan', 'Sosa', '2664444555', 'juan.sosa@example.com');
