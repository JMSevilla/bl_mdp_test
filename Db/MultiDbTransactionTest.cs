using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using WTW.MdpService.Infrastructure.Db;
using WTW.MdpService.Infrastructure.MdpDb;
using WTW.MdpService.Infrastructure.MemberDb;

namespace WTW.MdpService.Test.Db;

public class MultiDbTransactionTest
{
    private readonly Mock<IMemberDbUnitOfWork> _memberDbUnitOfWorkMock;
    private readonly Mock<IMdpUnitOfWork> _mdpUnitOfWorkMock;
    private readonly MultiDbTransaction _sut;

    public MultiDbTransactionTest()
    {
        _memberDbUnitOfWorkMock = new Mock<IMemberDbUnitOfWork>();
        _mdpUnitOfWorkMock = new Mock<IMdpUnitOfWork>();
        _sut = new MultiDbTransaction(_memberDbUnitOfWorkMock.Object, _mdpUnitOfWorkMock.Object);
    }

    public async Task BeginsTransaction()
    {
        await _sut.Begin();

        _memberDbUnitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);
    }

    public async Task RollsBackTransaction()
    {
        var memberDbTransactionMock = new Mock<IDbContextTransaction>();
        var mdpDbTransactionMock = new Mock<IDbContextTransaction>();

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(memberDbTransactionMock.Object);
        _mdpUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mdpDbTransactionMock.Object);
        await _sut.Begin();

        await _sut.Rollback();

        memberDbTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        mdpDbTransactionMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task RollBackThrows_WhenMemberDbTransactionIsNotStarted()
    {
        IDbContextTransaction memberDbTransactionMock = null;
        var mdpDbTransactionMock = new Mock<IDbContextTransaction>();        

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(memberDbTransactionMock);
        _mdpUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mdpDbTransactionMock.Object);
        await _sut.Begin();

        var action = async () => await _sut.Rollback();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Transaction not started");
    }

    public async Task RollBackThrows_WhenMdpDbTransactionIsNotStarted()
    {
        var memberDbTransactionMock = new Mock<IDbContextTransaction>();
        IDbContextTransaction mdpDbTransactionMock = null;

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(memberDbTransactionMock.Object);
        _mdpUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mdpDbTransactionMock);
        await _sut.Begin();

        var action = async () => await _sut.Rollback();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Transaction not started");
    }

    public async Task CommitsTransaction()
    {
        var memberDbTransactionMock = new Mock<IDbContextTransaction>();
        var mdpDbTransactionMock = new Mock<IDbContextTransaction>();

        _memberDbUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(memberDbTransactionMock.Object);
        _mdpUnitOfWorkMock
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(mdpDbTransactionMock.Object);
        await _sut.Begin();

        await _sut.Commit();

        memberDbTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        memberDbTransactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        mdpDbTransactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        mdpDbTransactionMock.Verify(x => x.DisposeAsync(), Times.Once);

        _memberDbUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
        _mdpUnitOfWorkMock.Verify(x => x.Commit(), Times.Once);
    }
}