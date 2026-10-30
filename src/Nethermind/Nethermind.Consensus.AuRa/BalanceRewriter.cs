// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using Nethermind.Consensus.AuRa.BalanceRecovery;
using Nethermind.Core.Specs;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.State;

namespace Nethermind.Consensus.AuRa;

/// <summary>
/// Native balance remaps baked into the binary (hard-fork release).
/// Applied on process/produce of each configured fork block; no node config file.
/// Policy: transfer min(amount, actual balance); warn and continue if capped. Does not touch nonce/code/storage.
/// </summary>
public class BalanceRewriter
{
    public const long ForkBlockNumber = BalanceHardForkSpec.BlockNumber;

    private readonly Dictionary<long, BalanceRecoveryConfig> _forksByBlock;

    public BalanceRewriter(BalanceRecoveryConfig config)
        : this([config])
    {
    }

    public BalanceRewriter(IReadOnlyList<BalanceRecoveryConfig> forks)
    {
        _forksByBlock = new Dictionary<long, BalanceRecoveryConfig>(forks.Count);
        foreach (BalanceRecoveryConfig fork in forks)
        {
            _forksByBlock[fork.BlockNumber] = fork;
        }
    }

    public static BalanceRewriter Create() => new(BalanceHardForkSpec.CreateForks());

    public void Apply(long blockNumber, IWorldState state, IReleaseSpec spec, ILogger logger)
    {
        if (!_forksByBlock.TryGetValue(blockNumber, out BalanceRecoveryConfig? config))
        {
            return;
        }

        if (logger.IsInfo) logger.Info($"Applying balance recovery at block {blockNumber}: {config.Transfers.Count} transfer(s).");

        foreach (BalanceRecoveryTransfer transfer in config.Transfers)
        {
            UInt256 balance = state.GetBalance(transfer.From);
            UInt256 actual = UInt256.Min(transfer.Amount, balance);

            if (actual < transfer.Amount)
            {
                if (logger.IsWarn)
                {
                    logger.Warn(
                        $"Balance recovery: transfer from {transfer.From} to {transfer.To} capped from {transfer.Amount} to {actual} at block {blockNumber} (available {balance}).");
                }
            }

            if (actual.IsZero)
            {
                continue;
            }

            state.SubtractFromBalance(transfer.From, actual, spec);
            state.AddToBalanceAndCreateIfNotExists(transfer.To, actual, spec);
        }

        if (logger.IsInfo) logger.Info("Balance recovery applied.");
    }
}
