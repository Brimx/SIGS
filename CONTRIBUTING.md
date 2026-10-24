# Cómo contribuir

## 1. Preparación

```bash
git clone https://github.com/Brimx/SIGS.git
cd SIGS
dotnet restore Supermercado.slnx
dotnet build Supermercado.slnx
dotnet test Supermercado.Pruebas/Supermercado.Pruebas.csproj
```

Requisitos: .NET SDK 10, Git y, opcionalmente, Rider o VS Code con C# Dev Kit.

## 2. Antes de escribir código

1. Lea [`docs/arquitectura.md`](docs/arquitectura.md) y [`docs/convenciones.md`](docs/convenciones.md).
2. Identifique la capa que corresponde al cambio:
   - Regla de negocio → `Supermercado.Entidades` o `Supermercado.Logica`.
   - Persistencia → `Supermercado.Datos`.
   - Pantalla → `Supermercado.GUI`.
3. Verifique si el cambio afecta permisos, modelo de datos o integraciones; en ese caso actualice la documentación correspondiente.

## 3. Ramas

```text
feat/<modulo>-<tema>
fix/<modulo>-<tema>
docs/<tema>
test/<tema>
ci/<tema>
refactor/<modulo>-<tema>
```

Ejemplos: `feat/pos-pago-mixto`, `fix/inventario-stock-negativo`, `docs/adr-postgresql`.

Una rama por bloque lógico. No mezcle refactor, feature y documentación no relacionada.

## 4. Commits

Formato convencional, en español, imperativo y corto:

```text
feat(ventas): reembolsar anuladas en el arqueo
fix(inventario): impedir stock negativo en el reparto
test(seguridad): cubrir bloqueo por empleado inactivo
docs(adr): documentar PostgreSQL local y Supabase
ci: compilar y probar en GitHub Actions
```

Prefijos: `feat`, `fix`, `refactor`, `test`, `docs`, `ci`, `chore`.

Reglas:

- Un commit = un cambio coherente.
- El mensaje explica **qué** y **por qué**, no el detalle línea a línea.
- No deje archivos temporales, binarios ni la base `sigs.db`.
- No commitee claves, tokens ni cadenas de conexión reales.

## 5. Reglas de código

- 4 espacios; llaves en línea nueva (Allman).
- Una clase pública por archivo, con el mismo nombre.
- `sealed` en las hojas de una jerarquía TPH.
- `async` con sufijo `Async`; no use `.Result` ni `.Wait()`.
- Inyecte `TimeProvider`; no use `DateTime.UtcNow`.
- Dinero en `long` (pesos); porcentajes en `int` (puntos básicos).
- Excepciones del catálogo, no `Exception` genérica.
- Comentarios en español y solo para explicar el **por qué**.
- Los servicios de dominio no guardan: el caso de uso llama `IUnidadDeTrabajo.GuardarAsync` una vez.

## 6. Pruebas

- Toda regla nueva o corregida necesita prueba.
- Use `RelojFijo` y repositorios en memoria; no toque la base real.
- Nombre: `Acción_Condición_Resultado`.
- No dependa de red, hora real ni orden de ejecución.
- Antes de abrir el PR:

```bash
dotnet build Supermercado.slnx -v q --nologo
dotnet test Supermercado.Pruebas/Supermercado.Pruebas.csproj --no-build --nologo
```

Criterio: **0 errores, 0 advertencias, 0 pruebas fallidas**.

## 7. Pull request

La descripción debe incluir:

1. **Qué cambia** y en qué módulos.
2. **Por qué** (issue, requisito o bug).
3. **Cómo se probó** (comandos y casos).
4. **Impacto** en modelo, permisos, documentación o integraciones.
5. Capturas si toca la GUI.

Checklist:

- [ ] Build sin advertencias.
- [ ] Pruebas en verde.
- [ ] Documentación actualizada si cambió el modelo, permisos o arquitectura.
- [ ] Sin secretos ni archivos generados.
- [ ] Commits con prefijo convencional.
- [ ] Rama actualizada con `main`.

## 8. Documentación que debe actualizarse

| Si cambia… | Actualice |
| :-- | :-- |
| Tabla, campo, índice o FK | [`docs/diccionario-datos.md`](docs/diccionario-datos.md) y [`docs/modelo-er.md`](docs/modelo-er.md) |
| Permiso o rol | [`docs/matriz-permisos.md`](docs/matriz-permisos.md) |
| Requisito funcional | [`docs/requisitos-trazabilidad.md`](docs/requisitos-trazabilidad.md) |
| Capa, flujo o integración | [`docs/arquitectura.md`](docs/arquitectura.md) |
| Decisión estructural | [`docs/decisiones.md`](docs/decisiones.md) (nuevo ADR) |
| Prueba o cobertura | [`docs/plan-pruebas.md`](docs/plan-pruebas.md) |
| Comando o despliegue | [`docs/despliegue.md`](docs/despliegue.md) |

## 9. Revisión de código

El revisor verifica:

- La regla vive en la capa correcta.
- No hay lógica de negocio en la GUI.
- Los permisos se validan en lógica, no solo en la vista.
- El dinero no usa `double` ni `decimal`.
- Las fechas salen de `TimeProvider`.
- Hay prueba del caso feliz y del caso de error.
- No se captura `Exception` genérica sin justificación.
- La documentación coincide con el código.

## 10. Reportar un bug

Incluya:

1. Pasos para reproducir.
2. Resultado esperado y resultado real.
3. Mensaje de excepción y traza.
4. Sistema operativo y versión de .NET.
5. Si aplica, el estado de la base (sin datos personales).

## 11. Licencia

Al contribuir acepta que su aporte se publique bajo la licencia MIT del repositorio.
