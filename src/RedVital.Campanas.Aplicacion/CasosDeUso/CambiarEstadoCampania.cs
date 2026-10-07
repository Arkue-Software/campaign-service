// RedVital — campaign-service
// T-312.4 — Publicación y cierre de campañas, solo rol U4 (admin_banco) de
// la institución organizadora.
//   POST /v1/campanias/{id}/publicacion : borrador  → publicada
//   POST /v1/campanias/{id}/cierre      : publicada → cerrada
// Repetir la misma operación devuelve el estado actual sin cambiar nada
// (idempotente, convención 2.4 del incremento). Toda operación, permitida o
// denegada, queda en registro_auditoria_camp (T-312.5).

using Microsoft.EntityFrameworkCore;
using RedVital.Campanas.Dominio;
using RedVital.Campanas.Infraestructura.Persistencia;

namespace RedVital.Campanas.Aplicacion.CasosDeUso;

public enum Accion { Publicar, Cerrar }

public class CambiarEstadoCampania
{
    private readonly CampanasDbContext _contexto;
    private readonly IRegistradorAuditoriaCamp _auditoria;

    public CambiarEstadoCampania(CampanasDbContext contexto, IRegistradorAuditoriaCamp auditoria)
    {
        _contexto = contexto;
        _auditoria = auditoria;
    }

    public async Task<Resultado<CampaniaRespuesta>> EjecutarAsync(Solicitante s, Guid id, Accion accion, string correlacionId, CancellationToken ct = default)
    {
        var operacion = accion == Accion.Publicar ? "publicar_campania" : "cerrar_campania";
        var (desde, hacia) = accion == Accion.Publicar ? ("borrador", "publicada") : ("publicada", "cerrada");

        if (!s.EsAdminBanco)
        {
            await _auditoria.RegistrarAsync(Auditar.Hecho(s, operacion, id, "denegado", correlacionId), ct);
            return Resultado<CampaniaRespuesta>.Error(Fallo.NoPermitido);
        }

        var c = await _contexto.Campanias.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return Resultado<CampaniaRespuesta>.Error(Fallo.NoEncontrado);

        if (!s.Alcanza(c))
        {
            await _auditoria.RegistrarAsync(Auditar.Hecho(s, operacion, id, "denegado", correlacionId,
                jurisdiccionSolicitada: $"institucion:{c.InstitucionId}"), ct);
            return Resultado<CampaniaRespuesta>.Error(Fallo.NoEncontrado);
        }

        if (c.Estado == hacia) // reenvío: no se repite el cambio
            return Resultado<CampaniaRespuesta>.Ok(CampaniaRespuesta.Desde(c));

        if (c.Estado != desde)
            return Resultado<CampaniaRespuesta>.Error(Fallo.Conflicto); // 409 conflicto-de-estado

        var ahora = DateTimeOffset.UtcNow;
        if (accion == Accion.Publicar && c.TerminaEn <= ahora)
            return Resultado<CampaniaRespuesta>.Error(Fallo.ReglaNegocio); // 422: no se publica una campaña ya terminada

        c.Estado = hacia;
        c.ActualizadaEn = ahora;
        if (accion == Accion.Publicar) c.PublicadaEn = ahora;
        await _contexto.SaveChangesAsync(ct);

        await _auditoria.RegistrarAsync(Auditar.Hecho(s, operacion, id, "permitido", correlacionId), ct);
        return Resultado<CampaniaRespuesta>.Ok(CampaniaRespuesta.Desde(c));
    }
}
