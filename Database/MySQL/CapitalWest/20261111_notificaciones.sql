-- Inbox de notificaciones vinculadas a acciones auditadas.
-- Ejecutar sobre la base capitalwest existente antes de usar el icono del header.
USE capitalwest;

CREATE TABLE IF NOT EXISTS Notificaciones (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  UsuarioId INT NOT NULL,
  Modulo VARCHAR(40) NOT NULL,
  Accion VARCHAR(80) NOT NULL,
  Titulo VARCHAR(140) NOT NULL,
  Mensaje VARCHAR(255) NOT NULL,
  CreadaUtc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  LeidaUtc DATETIME NULL,
  PRIMARY KEY (Id),
  KEY ix_notificaciones_usuario_fecha (UsuarioId, CreadaUtc, Id),
  KEY ix_notificaciones_usuario_lectura (UsuarioId, LeidaUtc),
  CONSTRAINT fk_notificaciones_usuario FOREIGN KEY (UsuarioId)
    REFERENCES Usuarios(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
