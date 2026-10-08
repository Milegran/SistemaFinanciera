-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Servidor: 127.0.0.1:3306
-- Tiempo de generación: 08-10-2026 a las 22:23:22
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
-- Base de datos: `personal`
--

-- --------------------------------------------------------

--
-- Estructura de tabla para la tabla `provincia`
--

CREATE TABLE `provincia` (
  `codigo_provincia` varchar(2) NOT NULL,
  `nombre_provincia` varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

--
-- Volcado de datos para la tabla `provincia`
--

INSERT INTO `provincia` (`codigo_provincia`, `nombre_provincia`) VALUES
('01', 'BOCAS DEL TORO'),
('02', 'COCLE'),
('03', 'COLON'),
('04', 'CHIRIQUI'),
('05', 'DARIEN'),
('06', 'HERRERA'),
('07', 'LOS SANTOS'),
('08', 'PANAMA'),
('09', 'VERAGUAS'),
('10', 'COMARCA GUNAYALA'),
('11', 'COMARCA EMBERA WOUNAAN'),
('12', 'COMARCA NGABE-BUGLE'),
('13', 'PANAMA OESTE');

--
-- Índices para tablas volcadas
--

--
-- Indices de la tabla `provincia`
--
ALTER TABLE `provincia`
  ADD UNIQUE KEY `codigo_provincia` (`codigo_provincia`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
