-- 20261112_metodos_expresiones.sql
-- Fórmulas propias para los métodos de cálculo.
-- Reemplaza la estrategia base (alias) por las expresiones que ejecuta
-- directamente el motor de la capa BL:
--   * ExprInteres / ExprCapital: obligatorias para los métodos nuevos
--     (los cinco métodos nativos las dejan en NULL).
--   * ExprSaldo: opcional; vacía significa "saldo - capital".
-- Los valores se validan con un calendario de prueba de 1, 3 y 12 cuotas
-- antes de guardarse (FinanzasServicio.CrearMetodoAdministracion).

ALTER TABLE MetodosCalculo
  DROP FOREIGN KEY fk_me_estrategia;

ALTER TABLE MetodosCalculo
  DROP COLUMN EstrategiaBase,
  ADD COLUMN ExprInteres VARCHAR(2000) NULL
    COMMENT 'Fórmula de interes por período; NULL en los métodos nativos' AFTER Variables,
  ADD COLUMN ExprCapital VARCHAR(2000) NULL
    COMMENT 'Fórmula de capital por período; NULL en los métodos nativos' AFTER ExprInteres,
  ADD COLUMN ExprSaldo VARCHAR(2000) NULL
    COMMENT 'Fórmula de saldo por período; vacía o NULL = saldo - capital' AFTER ExprCapital;
