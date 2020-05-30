
import * as Loading from '../commJS/Loading.jsx';
import {number_format} from '../commJS/common';

(function ($, React, ReactDOM, i18n, axios, moment) {
    const Status = {1:<div className="status-icon ace">Running</div>,
                    2:<div className="status-icon cal">Canceled</div>,
                    3:<div className="status-icon cal">Expiry</div>,
                    4:<div className="status-icon end">Fulfill</div>};   

    class PromotionItem extends React.Component {
        constructor(props) {
            super(props);
        }

        render() {
            let Data=this.props.Data;
            const diff = moment.duration(moment.unix(Data.periodTo).diff(new Date()));
            return (
                <a href={"/"+SiteLanguage+"/Promotion/Bonus?BonusCode="+Data.bonusCode} className="promotions-set">
                    <article>
                        <div style={{border: '0',display: 'inline-block',position: 'relative',display: 'block',width: '100%'}}>
                            <img src={CMSSkinRootPath+"Promotion/"+SiteLanguage+"/banner-"+Data.bonusCode+".jpg"} style={{display: 'block',width: '100%'}}/>
                        </div>
                        <div className="promotions-txt">
                            <ul>
                                <li className="txt-blue">
                                    {Data.title}
                                    {Data.bonusInfoStatus > 0 ? Status[Data.bonusInfoStatus] : null}
                                </li>
                                <li className="txt-gray">
                                    {Data.description}
                                </li>
                                <li>
                                    <div className="promotions-type">
                                        <ul className="txt-gray">
                                            <li>
                                                <i className="icon icon-transaction"></i>{i18n.lbl_Bonus}
                                                <span className="txt-org">{i18n["Currency_"+Data.currencyId]} {Data.bonus}% </span>
                                            </li>
                                            <li>
                                                <i className="icon icon-gototop"></i>{i18n.lbl_MaxBonusAmount}
                                                <span className="txt-org">{i18n["Currency_"+Data.currencyId]} {number_format(Data.maxBonus)}</span>
                                            </li>
                                            <li>
                                                <i className="icon icon-sport252"></i>{i18n.lbl_RollOver}
                                                <span className="txt-org">{Data.rollOver}x </span>
                                            </li>
                                            {Data.bonusInfoStatus!=1 ?
                                            <li>
                                                <i className="icon icon-race"></i>{i18n.lbl_ExpiryTime}
                                                <span className="txt-org">{diff.days()} {i18n.lbl_Days}{diff.hours()} {i18n.lbl_Hours}</span>
                                            </li>:<li>
                                                <i className="icon icon-race"></i>{i18n.lbl_RemainingTime}
                                                <span className="txt-org">{moment.unix(Data.periodTo).format('lll')}</span>                                            
                                            </li>}
                                            {Data.bonusInfoStatus==1 ?
                                            <li>
                                                <i className="icon icon-transaction"></i>{i18n.lbl_BonusAmount}
                                                <span className="txt-org">{number_format(Data.bonusAmount)}</span>
                                            </li>:null}                                            
                                        </ul>
                                    </div>                                
                                </li>
                                <li className="txt-org">{i18n.lbl_TC}</li>                                
                            </ul>
                        </div>
                    </article>
                    {Data.bonusInfoStatus==1 && Data.custRollOver!=null ? <article>
                        <div class="schedule-module">
                            <div class="promotions-txt">
                                <ul>
                                    <li class="txt-org">{i18n.lbl_BonusRollOver}</li>
                                    <li class="txt-gray">
                                        <span className="txt-org">{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.turnOver)}</span> (Req.{i18n["Currency_"+Data.currencyId]} ${number_format(Data.custRollOver.rollOver)})
                                        <div class="percentage">{(Math.floor((Data.custRollOver.turnOver / Data.custRollOver.rollOver) * 10000)/100).toFixed(2)}%</div>
                                    </li>
                                    <li>
                                        <div class="schedule">
                                            <div class="schedule-bg" style={{width:  (Data.custRollOver.turnOver / Data.custRollOver.rollOver) * 100 + "%"}}></div>
                                        </div>
                                    </li>
                                </ul>
                            </div>
                        </div>
                    </article>:null}    
                </a>
            )
        }
    }

    class Promotions extends React.Component {
        constructor(props) {
            super(props);
            this.state={Data: null};
        }

        componentDidMount() {
            this.getPromotionList();
        }        

        getPromotionList() {
            Loading.OnLoading();
            axios.post('/api/Promotion/GetPromotionList/'+ SiteLanguage)
                .then(function (response) {
                    Loading.UnLoading();
                    let result = response.data;
                    if (result.errorCode == 0) {
                        this.setState({ Data: result.data });
                    } else {
                        alert(result.message);
                    }
                }.bind(this)).catch(function (error) {
                    Loading.UnLoading();
                    alert(error);
                }.bind(this));
        }        

        render() {
            let PromotionList = [];
            let categoryId = 0;
            let iKey = 0;
            let data = this.state.Data;
            if(data != null) {
                for(let i=0; i < data.length; i++) {
                    let iData = data[i];
                    if(iData.categoryId != categoryId){
                        PromotionList.push(<span key={iKey} className="promotions-title">{iData.categoryName}</span>);
                        iKey++;
                    }

                    PromotionList.push(<PromotionItem key={iKey} Data={iData}/>);
                }
            }
            //
            return (
                <div className="promotions-module">
                    {PromotionList}
                </div>
            )
        }
    }

    ReactDOM.render(<Promotions />, document.getElementById('promotions'));
})(jQuery, React, ReactDOM, i18n, axios, moment);