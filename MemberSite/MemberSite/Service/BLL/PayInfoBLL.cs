using MemberSite.Extensions;
using MemberSite.Service.DAL;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class PayInfoBLL
    {
        private readonly PayService _payService;
        private readonly DepositLimitBLL _depositLimitBll;
        private readonly BankInfoOffBLL _bankInfoOffBll;
        private readonly BankInfoBLL _bankInfoBll;
        private readonly CustInfoBll _custInfoBll;
        private readonly ICacheHelper _redisHelper;

        public PayInfoBLL(IServiceProvider provider)
        {
            _payService = provider.GetRequiredService<PayService>();
            _custInfoBll = provider.GetRequiredService<CustInfoBll>();
            _depositLimitBll = provider.GetRequiredService<DepositLimitBLL>();
            _bankInfoOffBll = provider.GetRequiredService<BankInfoOffBLL>();
            _bankInfoBll = provider.GetRequiredService<BankInfoBLL>();
            _redisHelper = provider.GetRequiredService<ICacheHelper>();
        }

        private IEnumerable<PayGroupInfo> GetPayGroupWithCache()
        {
            var dbData = _redisHelper.TryGetOrCreate("PayGroupInfoList", TimeSpan.FromMinutes(5),
                () => _payService.GetPayGroup(), RedisDataBase.Shared);
            return dbData;
        }
        
        public IEnumerable<PayChannelInfo> GetPayChannelWithCache()
        {
            var dbData = _redisHelper.TryGetOrCreate("PayChannelInfoList", TimeSpan.FromMinutes(5),
                () => _payService.GetPayChannel(), RedisDataBase.Shared);
            return dbData;
        }

        private readonly Dictionary<DepositType, OfflineBankAccountSupportChannelEnum> _dicSupportChannelMapper
            = new()
            {
                {DepositType.OnlineBanking, OfflineBankAccountSupportChannelEnum.LocalBank},
                {DepositType.MobilePayment, OfflineBankAccountSupportChannelEnum.EWallet},
                {DepositType.QrPayPayment, OfflineBankAccountSupportChannelEnum.BankQR}
            };

        public IDictionary<string, PayMethod> GetPayInfoListAll(int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var result = new Dictionary<string, PayMethod>();

            var bankTransferData = _bankInfoOffBll.GetBankInfoOffListWithCache(custInfo.CustId)
                .Select(x => new OfflineBankInfo
                {
                    SysId = x.SysId,
                    BankName = x.BankName,
                    BankCode = x.BankCode,
                    BankLogo = x.BankLogo,
                    MaxAmount = x.MaxAmount,
                    MinAmount = x.MinAmount,
                    CanSubmit = x.CanSubmit,
                    BankAccountInfo = new OfflineBankAccount
                    {
                        BankId = x.BankId,
                        AccountNo = x.AccountNo,
                        AccountName = x.AccountName,
                        QrCodeUrl = x.QRCodeUrl
                    },
                    DepositType = DepositType.LocalBank
                });
            result.Add("BankTransfer", new PayMethod(bankTransferData));
            
            var depositTypes = new []
            {
                DepositType.OnlineBanking,
                DepositType.QuickTransfer
            };

            result.AddRange(GetOnlineBankMethods(depositTypes, custId));

            var line = _bankInfoBll.GetLineChannel(custInfo.SiteId, custInfo.CurrencyId);
            //塞假資料給luke接
            result.Add("LinePay", new PayMethod(new LinePay(line?.Link ?? "https://translate.google.com.tw/")));

            var autoChannelData = _bankInfoBll.GetAutoChannelBankList(custInfo, vipLevel)
                .GroupBy(x => x.SysId)
                .Select(x =>
                {
                    var bank = x.First();
                    return new AutoChannelBankInfo
                    {
                        BankLogo = bank.BankLogo,
                        BankCode = bank.BankCode,
                        BankName = bank.BankName,
                        SysId = bank.SysId,
                        MaxAmount = x.Max(y => y.MaxAmount),
                        MinAmount = x.Min(y => y.MinAmount),
                        DepositType = DepositType.BankAutoChannel
                    };
                });
            
            //塞假資料給luke接
            if (!autoChannelData.Any())
            {
                autoChannelData = new AutoChannelBankInfo[]
                {
                    new ()
                    {
                        BankLogo = "idbi.png",
                        BankName = "IDBI Bank",
                        BankCode = "IDBI",
                        SysId = 265,
                        MaxAmount = 50000,
                        MinAmount = 50,
                        DepositType = DepositType.BankAutoChannel
                    }
                };
            }
            
            result.Add(DepositType.BankAutoChannel.GetDescription(), new PayMethod(autoChannelData));
            
            depositTypes = new []
                {
                    DepositType.MobilePayment,
                    DepositType.LoadCardPayment,
                    DepositType.CryptoPayment,
                    DepositType.QrPayPayment
                };

            result.AddRange(GetGenericPayMethods(depositTypes, custId));

            return result;
        }

        public IDictionary<string, PayMethod> GetOnlineBankMethods(IEnumerable<DepositType> depositTypes, int custId)
        {
            var result = new Dictionary<string, PayMethod>();
            
            foreach (var depositType in depositTypes)
            {
                var data = _bankInfoBll.GetOnlinePaymentList(custId, depositType)
                    .Select(x => new OnlineBankChannel
                    {
                        ChannelId = x.ChannelId,
                        ChannelName = x.PayName,
                        PayId = x.PayId,
                        PayName = x.PayName,
                        SelectBank = x.SelBank,
                        Mode = x.Mode,
                        MaxAmount = x.MaxAmount,
                        MinAmount = x.MinAmount,
                        AmountRule = 1,
                        AmountList = Array.Empty<decimal>(),
                        SupportBanks = x.BankList.Select(y => new BankInfo
                        {
                            BankLogo = y.BankLogo,
                            BankCode = y.BankCode,
                            BankName = y.BankName,
                            SysId = y.SysId,
                            MaxAmount = x.MaxAmount,
                            MinAmount = x.MinAmount
                        }),
                        DepositType = depositType
                    });
                
                result.Add(depositType.GetDescription(), new PayMethod(data));
            }

            return result;
        }

        private IDictionary<string, PayMethod> GetGenericPayMethods(IEnumerable<DepositType> depositTypes, int custId)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);
            var payInfoList = GetPayInfoListWithCache(custInfo.SiteId)
                .Where(info => vipLevel >= info.PointLevel && info.CurrencyId == custInfo.CurrencyId);
            
            var depositLimitInfoList = _depositLimitBll.GetDepositLimitInfoListWithCache(custInfo.SiteId)
                ?.Where(x =>
                    x.SiteId == custInfo.SiteId &&
                    x.VipLevel == vipLevel &&
                    x.CurrencyId == custInfo.CurrencyId);
            
            var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId);
            
            var result = new Dictionary<string, PayMethod>();
            foreach (var depositType in depositTypes)
            {
                var limitInfo = depositLimitInfoList.FirstOrDefault(x => x.DepositType == depositType);
                
                if (limitInfo?.DepositStatus == 0)
                {
                    result.Add(depositType.GetDescription(), new PayMethod(Array.Empty<GenericPayGroup>()));
                    continue;
                }

                var payChannels = new List<GenericPayChannel>();
                var payInfos = payInfoList.Where(x => x.DepositType == depositType);

                if (payInfos.Any())
                {
                    var onlineChannels = payInfos.Select(x => new GenericPayChannel
                    {
                        ChannelId = x.ChannelId,
                        ChannelName = x.PayName,
                        PayId = x.PayId,
                        PayName = x.PayName,
                        Mode = x.Mode,
                        MaxAmount = x.MaxAmount,
                        MinAmount = x.MinAmount,
                        AmountRule = x.AmountRule,
                        AmountList = x.AmountList,
                        GroupId = x.GroupId,
                        GroupName = x.GroupName,
                        GroupSort = x.GSort,
                        ChannelSort = x.SSort,
                        DepositType = depositType
                    });
                    payChannels.AddRange(onlineChannels);
                }
                
                var payGroupInfos = GetPayGroupWithCache();

                #region Add LOCAL PAYMENT

                if (depositType == DepositType.CryptoPayment)
                {
                    var localCryptoList = _bankInfoOffBll.GetFilteredOfflineCryptoPayInfo(custId)
                        .GroupBy(x => x.ChannelName)
                        .Select(g =>
                            new GenericPayChannel
                            {
                                ChannelId = -1 * (int) g.First().CryptoCurrencyId,
                                ChannelName = g.Key,
                                Mode = (int) PayModeEnum.QrCode,
                                MaxAmount = g.Any(x => x.MaxAmount == 0)
                                    ? 0
                                    : g.Max(x => x.MaxAmount),
                                MinAmount = g.Min(x => x.MinAmount),
                                AmountRule = 1,
                                AmountList = Array.Empty<decimal>(),
                                GroupId = -1,
                                GroupName = "LetsPay",
                                PayName = "LetsPay",
                                ChannelSort = -1,
                                GroupSort = -1,
                                DepositType = DepositType.CryptoWallet
                            });
                    
                    payChannels.AddRange(localCryptoList);
                }
                else
                {
                    var localPayInfos = bankCards.Where(x =>
                        _dicSupportChannelMapper.ContainsKey(depositType)
                        && x.SupportChannel.Contains(_dicSupportChannelMapper[depositType]));
                
                    if (localPayInfos.Any())
                    {
                        var localPayChannels = localPayInfos.SelectMany(b => b.GroupIds,
                                (b, g) => new
                                {
                                    GroupId = g,
                                    b.MinAmount,
                                    b.MaxAmount
                                })
                            .GroupBy(x => x.GroupId)
                            .Where(x => payGroupInfos.Any(g => g.GroupId == x.Key && g.Type == depositType))
                            .Select(x =>
                            {
                                var bankCard = x.First();
                                var groupInfo = payGroupInfos.First(g => g.GroupId == x.Key);
                                return new GenericPayChannel
                                {
                                    ChannelId = bankCard.GroupId * -1,
                                    ChannelName = groupInfo.GroupName,
                                    Mode = (int) PayModeEnum.Redirect,
                                    MaxAmount = localPayInfos.Max(y => y.MaxAmount),
                                    MinAmount = localPayInfos.Min(y => y.MinAmount),
                                    AmountRule = 1,
                                    AmountList = Array.Empty<decimal>(),
                                    GroupId = bankCard.GroupId,
                                    GroupName = groupInfo.GroupName,
                                    GroupSort = groupInfo.Sort,
                                    ChannelSort = -1,
                                    DepositType = depositType
                                };
                            });
                        payChannels.AddRange(localPayChannels);
                    }
                }

                #endregion

                if (limitInfo != null)
                {
                    foreach (var payChannel in payChannels)
                    {
                        _depositLimitBll.FixDepositLimit(payChannel, limitInfo);
                    }
                }

                var payGroups = payChannels
                    .GroupBy(x => x.GroupId)
                    .Select(x =>
                    {
                        var f = x.First();
                        return new GenericPayGroup
                        {
                            GroupId = f.GroupId,
                            GroupName = f.GroupName,
                            Sort = f.GroupSort,
                            MaxAmount = x.Max(y => y.MaxAmount),
                            MinAmount = x.Min(y => y.MinAmount),
                            SupportChannels = x.Select(y => y)
                                .OrderBy(y => y.ChannelSort)
                        };
                    }).OrderBy(x => x.Sort);
                
                //塞假資料給luke接
                if (!payGroups.Any())
                {
                    payGroups = new List<GenericPayGroup>
                    {
                        new GenericPayGroup
                        {
                            GroupId = 150,
                            GroupName = "FakeGroup",
                            MaxAmount = 50000,
                            MinAmount = 50,
                            SupportChannels = new List<GenericPayChannel>
                            {
                                new GenericPayChannel
                                {
                                    ChannelId = 556,
                                    ChannelName = "FakeChannel",
                                    Mode = (int) PayModeEnum.Redirect,
                                    MaxAmount = 50000,
                                    MinAmount = 50,
                                    AmountRule = 1,
                                    AmountList = Array.Empty<decimal>(),
                                    GroupId = 150,
                                    GroupName = "FakeGroup",
                                },
                                new GenericPayChannel
                                {
                                    ChannelId = 557,
                                    ChannelName = "FakeChannel2",
                                    Mode = (int) PayModeEnum.Redirect,
                                    MaxAmount = 50000,
                                    MinAmount = 50,
                                    AmountRule = 1,
                                    AmountList = Array.Empty<decimal>(),
                                    GroupId = 150,
                                    GroupName = "FakeGroup",
                                }
                            }
                        }
                    }.OrderBy(x => x.Sort);
                }
                
                result.Add(depositType.GetDescription(), new PayMethod(payGroups));
            }

            return result;
        }




        public IEnumerable<PayGroup> GetPayGroupList(int custId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return new List<PayGroup>();
            }

            var payInfoList = GetPayInfoListWithCache(custInfo.SiteId, depositType)
                .Where(info => vipLevel >= info.PointLevel && info.CurrencyId == custInfo.CurrencyId);

            var payGroupList = payInfoList
                .Select(x => new PayGroup
                {
                    GroupId = x.GroupId,
                    GroupName = x.GroupName,
                    ChannelName = x.ChannelName,
                    GSort = x.GSort,
                    Mode = x.Mode,
                    MinAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderBy(y => y.MinAmount).First().MinAmount,
                    MaxAmount = payInfoList.Where(y => x.GroupId == y.GroupId).OrderByDescending(y => y.MaxAmount).First().MaxAmount
                })
                .Distinct(i => i.GroupId)
                .ToList();

            var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                .Where(x => _dicSupportChannelMapper.ContainsKey(depositType)
                            && x.SupportChannel.Contains(_dicSupportChannelMapper[depositType]));

            if (bankCards.Any())
            {
                var payGroupInfos = GetPayGroupWithCache();
                var payGroups = bankCards
                    .SelectMany(b => b.GroupIds,
                        (b, g) => new
                        {
                            GroupId = g,
                            b.MinAmount,
                            b.MaxAmount
                        })
                    .GroupBy(x => x.GroupId)
                    .Where(x => payGroupInfos.Any(g => g.GroupId == x.Key && g.Type == depositType))
                    .Select(x =>
                    {
                        var bankCard = x.First();
                        var groupInfo = payGroupInfos.First(g => g.GroupId == x.Key);
                        return new PayGroup
                        {
                            GroupId = bankCard.GroupId,
                            ChannelName = groupInfo.GroupName,
                            GroupName = groupInfo.GroupName,
                            Mode = (int)PayModeEnum.Redirect,
                            GSort = groupInfo.Sort,
                            MinAmount = bankCards.Min(x => x.MinAmount),
                            MaxAmount = bankCards.Max(x => x.MaxAmount)
                        };
                    });

                foreach (var payGroup in payGroups)
                {
                    var g = payGroupList.FirstOrDefault(x => x.GroupId == payGroup.GroupId);
                    if (g == null)
                    {
                        payGroupList.Add(payGroup);
                        continue;
                    }

                    g.MaxAmount = payGroup.MaxAmount > g.MaxAmount ? payGroup.MaxAmount : g.MaxAmount;
                    g.MinAmount = payGroup.MinAmount < g.MinAmount ? payGroup.MinAmount : g.MinAmount;
                }
            }

            if (depositLimitInfo != null)
            {
                foreach (var payGroup in payGroupList)
                {
                    _depositLimitBll.FixDepositLimit(payGroup, depositLimitInfo);
                }
            }

            return payGroupList.OrderBy(x => x.GSort);
        }

        public IEnumerable<PayInfo> GetPayInfoList(int custId, int groupId, DepositType depositType)
        {
            var custInfo = _custInfoBll.GetCustInfoWithCache(custId);
            var vipLevel = _custInfoBll.GetPointLevelWithCache(custId);

            var depositLimitInfo = _depositLimitBll.GetDepositLimitInfo(custId, depositType);
            if (depositLimitInfo?.DepositStatus == 0)
            {
                return Array.Empty<PayInfo>();
            }

            var payInfoList = GetPayInfoListWithCache(custInfo.SiteId, depositType)
                .Where(x => vipLevel >= x.PointLevel && x.CurrencyId == custInfo.CurrencyId && x.GroupId == groupId)
                .ToList();

            var bankCards = _bankInfoOffBll.GetOfflineBankCardListWithCache(custId)
                .Where(x => _dicSupportChannelMapper.ContainsKey(depositType)
                            && x.SupportChannel.Contains(_dicSupportChannelMapper[depositType])
                            && x.GroupIds.Contains(groupId));

            if (bankCards.Any())
            {
                var payGroupInfos = GetPayGroupWithCache();
                var groupInfo = payGroupInfos.First(g => g.GroupId == groupId);
                var payInfo = new PayInfo
                {
                    ChannelId = -1,
                    ChannelName = groupInfo.GroupName,
                    GSort = groupInfo.Sort,
                    SSort = -1,
                    Mode = (int)PayModeEnum.Redirect,
                    GroupId = groupId,
                    GroupName = groupInfo.GroupName,
                    MinAmount = bankCards.Min(x => x.MinAmount),
                    MaxAmount = bankCards.Max(x => x.MaxAmount)
                };

                payInfoList.Add(payInfo);
            }

            if (depositLimitInfo != null && payInfoList != null)
            {
                foreach (var payInfo in payInfoList)
                {
                    _depositLimitBll.FixDepositLimit(payInfo, depositLimitInfo);
                }
            }

            return payInfoList.Where(x => x.MaxAmount > 0)
                .OrderBy(x => x.GSort)
                .ThenBy(x => x.SSort);
        }

        public IEnumerable<PayInfo> GetPayInfoListWithCache(int siteId, DepositType? depositType = null)
        {
            var key = $"PaySetting:{siteId}:{depositType}";
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<PayInfo>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetPayInfo(siteId, depositType).OrderBy(x => x.GSort).ThenBy(x => x.SSort),
                RedisDataBase.Shared);
            return dbData;
        }
        
        public IEnumerable<WithdrawalBlockSetting> GetWithdrawalBlockSettingWithCache(int siteId, int currencyId)
        {
            var key = $"PaySetting:WithdrawalBlock:{siteId}";
            
            var dbData = _redisHelper.TryGetOrCreate<IEnumerable<WithdrawalBlockSetting>>(key, TimeSpan.FromMinutes(5),
                () => _payService.GetWithdrawalBlockSettingList(siteId));
            
            return dbData.Where(x => x.CurrencyId == currencyId);
        }
        
    }
}
