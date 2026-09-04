-- Base de datos e inicialización del proyecto Inmobiliaria_JM.
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

CREATE TABLE IF NOT EXISTS TiposInmueble (
    IdTipoInmueble INT UNSIGNED NOT NULL AUTO_INCREMENT,
    Nombre VARCHAR(80) NOT NULL,
    CONSTRAINT PK_TiposInmueble PRIMARY KEY (IdTipoInmueble),
    CONSTRAINT UQ_TiposInmueble_Nombre UNIQUE (Nombre)
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS Inmuebles (
    IdInmueble INT UNSIGNED NOT NULL AUTO_INCREMENT,
    IdPropietario INT UNSIGNED NOT NULL,
    IdTipoInmueble INT UNSIGNED NOT NULL,
    Direccion VARCHAR(200) NOT NULL,
    Cupo INT UNSIGNED NOT NULL,
    Coordenadas VARCHAR(100) NOT NULL,
    PrecioDia DECIMAL(12, 2) NOT NULL,
    Disponible TINYINT(1) NOT NULL DEFAULT 1,
    ImagenPortada VARCHAR(255) NULL,
    CONSTRAINT PK_Inmuebles PRIMARY KEY (IdInmueble),
    CONSTRAINT FK_Inmuebles_Propietarios FOREIGN KEY (IdPropietario)
        REFERENCES Propietarios (IdPropietario)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Inmuebles_TiposInmueble FOREIGN KEY (IdTipoInmueble)
        REFERENCES TiposInmueble (IdTipoInmueble)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT CK_Inmuebles_Cupo CHECK (Cupo > 0),
    CONSTRAINT CK_Inmuebles_PrecioDia CHECK (PrecioDia > 0)
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS Reservas (
    IdReserva INT UNSIGNED NOT NULL AUTO_INCREMENT,
    IdInmueble INT UNSIGNED NOT NULL,
    IdInquilino INT UNSIGNED NOT NULL,
    FechaDesde DATE NOT NULL,
    FechaHasta DATE NOT NULL,
    MontoDia DECIMAL(12, 2) NOT NULL,
    FechaTerminacionAnticipada DATE NULL,
    MontoMulta DECIMAL(12, 2) NULL,
    CONSTRAINT PK_Reservas PRIMARY KEY (IdReserva),
    CONSTRAINT FK_Reservas_Inmuebles FOREIGN KEY (IdInmueble)
        REFERENCES Inmuebles (IdInmueble)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT FK_Reservas_Inquilinos FOREIGN KEY (IdInquilino)
        REFERENCES Inquilinos (IdInquilino)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,
    CONSTRAINT CK_Reservas_Fechas CHECK (FechaHasta > FechaDesde),
    CONSTRAINT CK_Reservas_MontoDia CHECK (MontoDia > 0),
    CONSTRAINT CK_Reservas_MontoMulta CHECK (MontoMulta IS NULL OR MontoMulta >= 0),
    INDEX IX_Reservas_Inmueble_Fechas (IdInmueble, FechaDesde, FechaHasta)
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

INSERT IGNORE INTO TiposInmueble (Nombre)
VALUES
    ('Casa'),
    ('Departamento'),
    ('Monoambiente'),
    ('Loft');

INSERT INTO Inmuebles
    (IdPropietario, IdTipoInmueble, Direccion, Cupo,
     Coordenadas, PrecioDia, Disponible, ImagenPortada)
SELECT
    p.IdPropietario,
    t.IdTipoInmueble,
    'Av. Illia 125, San Luis',
    4,
    '-33.3017, -66.3378',
    45000.00,
    1,
    NULL
FROM Propietarios p
INNER JOIN TiposInmueble t ON t.Nombre = 'Departamento'
WHERE p.Dni = '20123456'
  AND NOT EXISTS (
      SELECT 1
      FROM Inmuebles i
      WHERE i.Direccion = 'Av. Illia 125, San Luis'
  );

INSERT INTO Inmuebles
    (IdPropietario, IdTipoInmueble, Direccion, Cupo,
     Coordenadas, PrecioDia, Disponible, ImagenPortada)
SELECT
    p.IdPropietario,
    t.IdTipoInmueble,
    'Las Heras 840, San Luis',
    2,
    '-33.2950, -66.3356',
    32000.00,
    1,
    NULL
FROM Propietarios p
INNER JOIN TiposInmueble t ON t.Nombre = 'Monoambiente'
WHERE p.Dni = '22987654'
  AND NOT EXISTS (
      SELECT 1
      FROM Inmuebles i
      WHERE i.Direccion = 'Las Heras 840, San Luis'
  );

INSERT INTO Reservas
    (IdInmueble, IdInquilino, FechaDesde, FechaHasta, MontoDia,
     FechaTerminacionAnticipada, MontoMulta)
SELECT
    i.IdInmueble,
    iq.IdInquilino,
    '2026-10-10',
    '2026-10-15',
    i.PrecioDia,
    NULL,
    NULL
FROM Inmuebles i
INNER JOIN Inquilinos iq ON iq.Dni = '30111222'
WHERE i.Direccion = 'Av. Illia 125, San Luis'
  AND NOT EXISTS (
      SELECT 1
      FROM Reservas r
      WHERE r.IdInmueble = i.IdInmueble
        AND r.IdInquilino = iq.IdInquilino
        AND r.FechaDesde = '2026-10-10'
        AND r.FechaHasta = '2026-10-15'
  );
