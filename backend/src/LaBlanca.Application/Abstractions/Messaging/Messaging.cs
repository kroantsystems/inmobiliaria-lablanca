using MediatR;

namespace LaBlanca.Application.Abstractions.Messaging;

/// <summary>Marca operações mutativas: passam pelo <c>TransactionBehavior</c>.</summary>
public interface IBaseCommand;

public interface ICommand : IRequest, IBaseCommand;

public interface ICommand<out TResponse> : IRequest<TResponse>, IBaseCommand;

/// <summary>Leitura sem efeitos colaterais.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;
