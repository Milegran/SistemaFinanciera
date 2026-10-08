# Ubicación suministrada

Para una base nueva, importe **../CapitalWest/CapitalWest.sql** (incluye los catálogos).
Si ya importó capitalwest, importe solamente **ActualizarUbicacion.sql** en phpMyAdmin. No vuelva a importar el esquema completo.

Se usan `provincia`, `distrito` y `corregimiento`, con los nombres, columnas y códigos originales: 13, 82 y 668 registros. `Originales/` conserva los archivos recibidos sin modificar. `provincia.sql` sustituye al archivo vacío `provincia2.sql`. Los códigos son texto para conservar los ceros iniciales. Solo se uniforma la codificación de provincia a utf8mb4 para las relaciones, y se añaden índices de jerarquía.

Los clientes guardan CodigoProvincia, CodigoDistrito y CodigoCorregimiento; las listas dependen de la selección anterior y el servidor verifica la jerarquía. Los nombres guardados se obtienen del catálogo.

BRILLANTE (130601) permanece en corregimiento. Su distrito 1306 falta en el archivo recibido, por eso no se ofrece en el formulario. No se inventó ni se eliminó ese dato.

La actualización conserva clientes, nombres de direcciones y antiguos códigos numéricos; archiva las tres tablas anteriores con el sufijo Anterior. Solo vincula automáticamente las direcciones cuya jerarquía completa coincide. La consulta final muestra clientes pendientes: seleccione su ubicación al editarlos.
