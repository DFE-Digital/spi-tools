using MediatR;

namespace Dfe.Spi.LocalPreparer.Services.Mediator.Storage.Command;

public class CopyTablesCommand : IRequest<bool>
{

    public class Handler : IRequestHandler<CopyTablesCommand, bool>
    {
        private readonly IMediator _mediator;

        public Handler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(CopyTablesCommand request,
            CancellationToken cancellationToken)
        {
            await _mediator.Send(new CopyTableToBlobCommand(), cancellationToken);
            await _mediator.Send(new CopyBlobToTableCommand(), cancellationToken);
            return true;
        }
    }
}