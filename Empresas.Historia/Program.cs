var evento1 = new EmpresaRegistrada("Constructora Andes", "Basico");
var evento2 = new PlanCambiado("Premium");
var evento3 = new EmpresaSuspendida("Falta de pago");
var evento4 = new EmpresaReactivada();
var evento5 = new EmpresaSuspendida("Incumplimiento de contrato");

var streamEventos = new List<object>() { evento1, evento2, evento3, evento4, evento5, evento4 };
var andes = new Empresa(streamEventos);

Console.WriteLine($"{andes.nombre}: plan {andes.plan}, {(andes.suspendida ? "suspendida" : "activa")}, reactivada {andes.reactivaciones} vez/veces");

//Clases
public class Empresa : AgregateRoot
{
    public string nombre {get; private set;} = "";
    public string plan {get; private set;}= "";
    public bool suspendida {get; private set;} = false;
    public int reactivaciones {get; private set;} = 0;

    public Empresa(List<object> streamEventos)
    {
       Load(streamEventos);
    }

    protected override void Aplicar(Object evento)
    {
        switch (evento)
        {
            case EmpresaRegistrada r:
                nombre = r.Nombre;
                plan = r.Plan;
                break;
            case PlanCambiado c:
                plan = c.PlanNuevo;
                break;
            case EmpresaSuspendida s:
                suspendida = true;
                break;
            case EmpresaReactivada er:
                suspendida = false;
                reactivaciones++;
                break;
        }
    }
}

public abstract class AgregateRoot
{
    public void Load(IEnumerable<Object> stream)
    {
        foreach (var evento in stream)
        {
            Aplicar(evento);
        }
    }

    protected abstract void Aplicar(Object evento);
}

//eventos
public record EmpresaRegistrada(string Nombre, string Plan);
public record PlanCambiado(string PlanNuevo);
public record EmpresaSuspendida(string Motivo);
public record EmpresaReactivada();
