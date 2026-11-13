-- Estandariza los códigos actuales de productos con el formato PRO-000001.
-- Los vínculos históricos apuntan a ProductosFinancieros.Id y no se modifican.
START TRANSACTION;

-- Evita colisiones con códigos PRO ya existentes durante el cambio de nomenclatura.
UPDATE ProductosFinancieros
SET Codigo = CONCAT('TMP-', LPAD(CAST(Id AS CHAR), 10, '0'), '-', LEFT(REPLACE(UUID(), '-', ''), 12));

UPDATE ProductosFinancieros
SET Codigo = CONCAT(
    'PRO-',
    LPAD(CAST(Id AS CHAR), GREATEST(6, CHAR_LENGTH(CAST(Id AS CHAR))), '0')
);

COMMIT;
