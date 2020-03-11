import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios) {
var _this = this;
  var GetRolloverPanelData = function() {
    axios
      .post("/api/Account/GetRolloverPanelData")
      .then(
        function(response) {
          var result = response.data;
          if (result.errorCode == 0) {
            if(result.data.promotions != null && result.data.promotions > 0) {
                $(".Js-dropdown-rollover").show();

            } else {
                $(".Js-dropdown-rollover").hide();
            }
          }

          setTimeout(GetRolloverPanelData.bind(this), 30000);
        }.bind(_this)
      )
      .catch(
        function(error) {
          setTimeout(GetRolloverPanelData.bind(this), 30000);
        }.bind(_this)
      );
  };

    $(function() {
        setTimeout(GetRolloverPanelData.bind(this), 30000);
    });

    class RolloverPanel extends React.Component {
        constructor(props) {
            super(props);
        }
    
        componentDidMount() {
            
        }
    
        render() {
            let amount = this.state.amount;

            return (
                <div className="rollover-info">

                    <div className="rollover-info__item">
                        <span className="text">@promotion.Title</span>
                        <div className="schedule">
                            <div className="schedule-bg" style="width: @(tools.Floor((promotion.custRollOver.TurnOver / promotion.custRollOver.RollOver*100),2).ToString("0.##"))%"></div>
                        </div>
                        <div className="schedule-info">
                            <span className="text">@selLangBLL.GetResourceValue($"Currency_{CurrencyId}") $@promotion.custRollOver.TurnOver.ToString("#,#0.##")</span>
                            <span className="text">(@selLangBLL.GetResourceValue("lbl_Req") @selLangBLL.GetResourceValue($"Currency_{CurrencyId}")) $@promotion.custRollOver.RollOver.ToString("#,#0.##")) @((Math.Floor(totalturnover / totalRollover * 10000)/100).ToString("0.##"))%</span>
                        </div>
                    </div>
                    
                </div>
            )
        }
    }
    let _balancePanel = <BalancePanel/>;
    ReactDOM.render(_balancePanel, document.getElementById('RolloverPanel'));
    //ReactDOM.render(_balancePanel, document.getElementById('RolloverPanel1'));
    
    })(jQuery, React, ReactDOM, i18n, axios);