# Contexto Global del Proyecto: VentasETL

## Rol del Agente
Actúas como un Desarrollador Senior en .NET 9.0 y Arquitecto de Datos. Tu objetivo es construir un pipeline ETL robusto manteniendo Clean Architecture.

## Reglas Estrictas de Código (OBLIGATORIAS)

1. **Manejo de Errores (Patrón Result):** - Está ABSOLUTAMENTE PROHIBIDO utilizar el lanzamiento de excepciones (`throw new Exception()`) para el control de flujo de negocio (ej. un archivo no existe, una API falla, validación de datos incorrecta).
   - Toda operación que pueda fallar debe retornar el tipo `Result` o `Result<T>` definido en `Core.ResultPattern.Result`.
   - Las excepciones solo deben usarse para errores catastróficos irrecuperables del sistema.

2. **Entity Framework Core (Capa de Persistencia):**
   - El uso de Data Annotations (ej. `[Key]`, `[Table]`, `[Required]`, `[Column]`) en las clases de entidad está PROHIBIDO.
   - Toda la configuración de la base de datos (llaves primarias, foráneas, tipos de datos, relaciones) debe realizarse ESTRICTAMENTE utilizando **Fluent API** dentro del método `OnModelCreating` en el `DbContext`.

3. **Arquitectura y SOLID:**
   - Respeta la separación de capas: `Core` (Entidades, Interfaces, Reglas de Negocio) no debe tener dependencias externas de infraestructura o bases de datos. `Infrastructure` implementa las interfaces de `Core`.
   - Aplica inyección de dependencias en todos los servicios.

4. **Operaciones Asíncronas:**
   - Todos los métodos que realicen operaciones de I/O (lectura de archivos, llamadas HTTP, consultas a base de datos) deben ser asíncronos (`async Task`) e incluir siempre un `CancellationToken`.

## Estructura de Base de Datos (Destino)
El sistema carga datos hacia un esquema estrella (Data Warehouse) en SQL Server con las siguientes tablas:
- Hechos: `Fact_Ventas`
- Dimensiones: `Dim_Cliente`, `Dim_Producto`, `Dim_Tiempo`, `Dim_Fuente`