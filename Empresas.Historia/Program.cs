var stream = new EventStream<Empresa>();

//Handler
var registrarEmpresaHandler = new RegistrarEmpresaHandler(stream);
var cambiarPlanHandler = new CambiarPlanHandler(stream);
var suspenderHandler = new SuspenderHandler(stream);
var reactivarHandler = new ReactivarHandler(stream);

//Commands
var commandoRegistrarEmpresa = new RegistrarEmpresa("Constructora Andes", "Basico");
var commandoSuspenderNoPago = new Suspender("Falta de pago");
var commandoReactivarUno= new Reactivar();
var commandoCambiarPlan= new CambiarPlan("Premium");
var commandoSuspenderIncumplimiento = new Suspender("Falta de pago");
var commandoReactivarDos= new Reactivar();

//Register command handler
var desparchador = new Despachador();
desparchador.Registrar<RegistrarEmpresa>(registrarEmpresaHandler);
desparchador.Registrar(suspenderHandler);
desparchador.Registrar(reactivarHandler);
desparchador.Registrar(cambiarPlanHandler);

//Enviar command
desparchador.Enviar(commandoRegistrarEmpresa);
desparchador.Enviar(commandoSuspenderNoPago);
desparchador.Enviar(commandoReactivarUno);
desparchador.Enviar(commandoCambiarPlan);
desparchador.Enviar(commandoSuspenderIncumplimiento);
desparchador.Enviar(commandoReactivarDos);

var andes2Version = stream.Get();

Console.WriteLine($"{andes2Version.nombre}: plan {andes2Version.plan}, {(andes2Version.suspendida ? "suspendida" : "activa")}, reactivada {andes2Version.reactivaciones} vez/veces");

//Clases
public class Empresa : AgregateRoot
{
    public string nombre {get; private set;} = "";
    public string plan {get; private set;}= "";
    public bool suspendida {get; private set;} = false;
    public int reactivaciones {get; private set;} = 0;

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

    public EmpresaRegistrada Registrar(string Nombre, string Plan) => new EmpresaRegistrada(Nombre, Plan);

    public PlanCambiado CambiarPlan(string PlanNuevo)
    {
        if(suspendida)
            throw new Exception("Empresa esta suspendida");

        return new(PlanNuevo);   
    }

    public EmpresaSuspendida? Suspender(string Motivo)
    {
        return suspendida? null: new(Motivo);
    }

    public EmpresaReactivada Reactivar() => new();
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

public class EventStream<T> where T : AgregateRoot, new()
{
    private readonly List<Object> _stream = new();

    public T Get()
    {
        var miEmpresa = new T();
        miEmpresa.Load(_stream);

        return miEmpresa;
    }

    public void Append(Object evento)
    {
        _stream.Add(evento);
    }
}

public interface ICommandHandler<TCommand>
{ 
    public void Handle(TCommand command);
}

public class RegistrarEmpresaHandler(EventStream<Empresa> stream) : ICommandHandler<RegistrarEmpresa>
{
    public void Handle(RegistrarEmpresa registrarEmpresa)
    {
        var andes = stream.Get();
        stream.Append(andes.Registrar(registrarEmpresa.Nombre, registrarEmpresa.Plan));
    }
}

public class CambiarPlanHandler(EventStream<Empresa> stream) : ICommandHandler<CambiarPlan>
{
    public void Handle(CambiarPlan cambiarPlan)
    {
        var andes = stream.Get();
        stream.Append(andes.CambiarPlan(cambiarPlan.plan));
    }
}

public class SuspenderHandler(EventStream<Empresa> stream) : ICommandHandler<Suspender>
{
    public void Handle(Suspender suspender)
    {
        var andes = stream.Get();
        var empresaSuspendida2 = andes.Suspender(suspender.motivo);
        if(empresaSuspendida2 is not null)
        {
            stream.Append(empresaSuspendida2); 
        }       
    }
}

public class ReactivarHandler(EventStream<Empresa> stream) : ICommandHandler<Reactivar>
{
    public void Handle(Reactivar reactivar)
    {
        var andes = stream.Get();
        stream.Append(andes.Reactivar());
    }
}

public class Despachador
{
    private Dictionary<Type , Action<object>> handlers = new();
    public void Registrar<T>(ICommandHandler<T> handler) => handlers.Add(typeof(T), (object command) => handler.Handle((T)command));

    public void Enviar(object command) => handlers[command.GetType()](command);
}

//eventos
public record EmpresaRegistrada(string Nombre, string Plan);
public record PlanCambiado(string PlanNuevo);
public record EmpresaSuspendida(string Motivo);
public record EmpresaReactivada();

//commands
public record RegistrarEmpresa(string Nombre, string Plan);
public record CambiarPlan(string plan);
public record Suspender(string motivo);
public record Reactivar();