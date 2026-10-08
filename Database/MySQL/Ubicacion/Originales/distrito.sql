-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1:3306
-- Tiempo de generación: 11-08-2026 a las 07:24:38
-- Versión del servidor: 10.4.32-MariaDB
-- Versión de PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de datos: `gestion_general`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `distrito`
--

CREATE TABLE `distrito` (
  `codigo_provincia` varchar(2) NOT NULL,
  `codigo_distrito` varchar(4) NOT NULL,
  `codigo` varchar(2) NOT NULL,
  `nombre_distrito` varchar(150) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Volcado de datos para la tabla `distrito`
--

INSERT INTO `distrito` (`codigo_provincia`, `codigo_distrito`, `codigo`, `nombre_distrito`) VALUES
('01', '0101', '01', 'BOCAS DEL TORO '),
('01', '0102', '02', 'CHANGUINOLA'),
('01', '0103', '03', 'CHIRIQUI GRANDE'),
('01', '0104', '04', 'ALMIRANTE'),
('02', '0201', '01', 'AGUADULCE'),
('02', '0202', '02', 'ANTON'),
('02', '0203', '03', 'LA PINTADA'),
('02', '0204', '04', 'NATA'),
('02', '0205', '05', 'OLA'),
('02', '0206', '06', 'PENONOME'),
('03', '0301', '01', 'COLÓN'),
('03', '0302', '02', 'CHAGRES'),
('03', '0303', '03', 'DONOSO'),
('03', '0304', '04', 'PORTOBELO'),
('03', '0305', '05', 'SANTA ISABEL'),
('03', '0306', '06', 'OMAR TORRIJOS HERRERA'),
('04', '0401', '01', 'ALANJE'),
('04', '0402', '02', 'BARU'),
('04', '0403', '03', 'BOQUERON'),
('04', '0404', '04', 'BOQUETE'),
('04', '0405', '05', 'BUGABA'),
('04', '0406', '06', 'DAVID'),
('04', '0407', '07', 'DOLEGA'),
('04', '0408', '08', 'GUALACA'),
('04', '0409', '09', 'REMEDIOS'),
('04', '0410', '10', 'RENACIMIENTO'),
('04', '0411', '11', 'SAN FELIX'),
('04', '0412', '12', 'SAN LORENZO'),
('04', '0413', '13', 'TOLÉ'),
('04', '0414', '14', 'TIERRAS ALTAS '),
('05', '0501', '01', 'CHEPIGANA'),
('05', '0502', '02', 'PINOGANA'),
('05', '0503', '03', 'SANTA FE'),
('06', '0601', '01', 'CHITRE'),
('06', '0602', '02', 'LAS MINAS'),
('06', '0603', '03', 'LOS POZOS'),
('06', '0604', '04', 'OCÚ'),
('06', '0605', '05', 'PARITA'),
('06', '0606', '06', 'PESE'),
('06', '0607', '07', 'SANTA MARIA'),
('07', '0701', '01', 'GUARARE'),
('07', '0702', '02', 'LAS TABLAS'),
('07', '0703', '03', 'LOS SANTOS'),
('07', '0704', '04', 'MACARACAS'),
('07', '0705', '05', 'PEDASI'),
('07', '0706', '06', 'POCRI'),
('07', '0707', '07', 'TONOSI'),
('08', '0801', '01', 'PANAMÁ'),
('08', '0802', '02', 'BALBOA'),
('08', '0803', '03', 'SAN MIGUELITO'),
('08', '0804', '04', 'TABOGA'),
('08', '0805', '05', 'CHEPO'),
('08', '0806', '06', 'CHIMAN'),
('09', '0901', '01', 'ATALAYA'),
('09', '0902', '02', 'CALOBRE'),
('09', '0903', '03', 'CAÑAZAS'),
('09', '0904', '04', 'LA MESA'),
('09', '0905', '05', 'LAS PALMAS'),
('09', '0906', '06', 'MONTIJO'),
('09', '0907', '07', 'RIO DE JESUS'),
('09', '0908', '08', 'SAN FRANCISCO'),
('09', '0909', '09', 'SANTA FE'),
('09', '0910', '10', 'SANTIAGO'),
('09', '0911', '11', 'SONA'),
('09', '0912', '12', 'MARIATO'),
('10', '1001', '01', 'COMARCA KUNA YALA'),
('11', '1101', '01', 'CEMACO'),
('11', '1102', '02', 'SAMBU'),
('12', '1201', '01', 'BESIKO'),
('12', '1202', '02', 'MIRONO'),
('12', '1203', '03', 'MUNA'),
('12', '1204', '04', 'NOLE DUIMA'),
('12', '1205', '05', 'ÑURUM'),
('12', '1206', '06', 'KANKINTU'),
('12', '1207', '07', 'KUSAPIN'),
('12', '1208', '08', 'JIRONDAI'),
('12', '1209', '09', 'SANTA CATALINA O CALOVÉDORA'),
('13', '1301', '01', 'ARRAIJAN'),
('13', '1302', '02', 'LA CHORRERA'),
('13', '1303', '03', 'CAPIRA'),
('13', '1304', '04', 'CHAME'),
('13', '1305', '05', 'SAN CARLOS');

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `distrito`
--
ALTER TABLE `distrito`
  ADD UNIQUE KEY `llave` (`codigo_distrito`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
