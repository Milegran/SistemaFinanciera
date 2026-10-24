-- Migración única de CapitalWest original (40 tablas) al alcance reducido (27).
-- Requiere respaldo completo previo. No usar para crear una base nueva.
-- DDL en MariaDB hace COMMIT implícito: esta migración no es transaccional.
-- Valida TODAS las tablas operativas retiradas antes del primer DROP TABLE.
-- Los únicos datos descartables son catálogos de permisos/reportes de la versión anterior.
USE capitalwest;
DELIMITER $$
CREATE PROCEDURE migrar_capitalwest_27()
BEGIN
  IF (SELECT COUNT(*) FROM `EjecucionesReporte`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: EjecucionesReporte contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `RecuperacionesClave`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: RecuperacionesClave contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `SesionesUsuario`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: SesionesUsuario contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `DetallesAmortizacionHistorica`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: DetallesAmortizacionHistorica contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `VersionesAmortizacion`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: VersionesAmortizacion contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `HistorialSolicitudes`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: HistorialSolicitudes contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `HistorialPrestamos`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: HistorialPrestamos contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `PreferenciasUsuario`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: PreferenciasUsuario contiene datos; preparar traslado antes de retirar';
  END IF;
  IF (SELECT COUNT(*) FROM `Notificaciones`) <> 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Migracion detenida: Notificaciones contiene datos; preparar traslado antes de retirar';
  END IF;
  DROP TABLE `EjecucionesReporte`;
  DROP TABLE `RolReportes`;
  DROP TABLE `Reportes`;
  DROP TABLE `RolPermisos`;
  DROP TABLE `Permisos`;
  DROP TABLE `RecuperacionesClave`;
  DROP TABLE `SesionesUsuario`;
  DROP TABLE `DetallesAmortizacionHistorica`;
  DROP TABLE `VersionesAmortizacion`;
  DROP TABLE `HistorialSolicitudes`;
  DROP TABLE `HistorialPrestamos`;
  DROP TABLE `PreferenciasUsuario`;
  DROP TABLE `Notificaciones`;
END$$
DELIMITER ;
CALL migrar_capitalwest_27();
DROP PROCEDURE migrar_capitalwest_27;
