import * as Loading from '../../commJS/Loading.jsx';
import {Pagination} from '../../commJS/Pagination.jsx';
import {number_format} from '../../commJS/common';
import {QuickPicker, QuickPickDefs} from '../component/DateQuickPicker'

(function ($, React, ReactDOM, i18n, axios, moment) {
    const Header = ()=> <div className="heading"><span className="text">{i18n.lbl_Statement}</span></div>

    const Filter = (props)=>{
        return(
            <form>
                <div className="form-row form-row--bg">
                    <QuickPicker 
                        QuickPickOnSelect={props.QuickPickOnSelect}
                        QuickPick={props.QuickPick}
                    />
                    <div className="form-group flex-3">
                        <label className="form-title">{i18n.lbl_Date}</label>
                        <div className="form-content form-content--date">
                            <DatePicker
                                displayText={i18n.lbl_Form}
                                Date={props.StartDate}
                                OnChange={props.SetStartDate}
                            />
                            <DatePicker
                                displayText={i18n.lbl_To}
                                Date={props.EndDate}
                                OnChange={props.SetEndDate}
                            />
                        </div>
                    </div>
                </div>
                <div className="btn-group">
                    <button className="btn btn-secondary" type="button" onClick={props.QueryData}>
                        <div className="btn__inner">
                            <span className="text">{i18n.lbl_Submit}</span>
                        </div>
                    </button>
                </div>
            </form>
        )
    }

    const DatePicker = (props)=>{
        return(
            <label className="textfield">
                <span className="text">{props.displayText}</span>
                <input type="date" className="textfield__input" value={props.Date} onChange={props.OnChange} max={moment().format('YYYY-MM-DD')}/>
                <i className="icon icon-calendar"></i>
                <span className="textfield__bg"></span>
            </label>
        )
    }

    const ChangeAmountFormat = (amount, direction)=>{
        return `${direction==1 ? '+' : '-'}${number_format(amount.toFixed(2))}`
    }

    const AmountFormat = (amount)=>{
        return number_format(amount.toFixed(2));
    }

    const DateFormat = (time)=>{
        let dateMoment = moment(time);
        return `${dateMoment.format('l')} (${dateMoment.format('Z')})`;
    }
    
    const TimeFormat = (time)=>{
        return moment(time).format("hh:mm:ss A");
    }

    const columnDefs = [
        {datacolumn:'createTime', displayText:i18n.lbl_Date, className:"td-time", format:{func:DateFormat}},
        {datacolumn:'createTime', displayText:i18n.lbl_Time, className:"td-date", format:{func:TimeFormat}},
        {datacolumn:'typeName', displayText:i18n.lbl_TypeName, className:"td-id"},
        {datacolumn:'changeAmount', displayText:i18n.lbl_ChangeAmount, className:"td-amount", format:{ func:ChangeAmountFormat, bindDatacolumn: 'direction'}},
        {datacolumn:'beforeAmount', displayText:i18n.lbl_BeforeAmount, className:"td-amount", format:{ func:AmountFormat }},
        {datacolumn:'afterAmount', displayText:i18n.lbl_AfterAmount, className:"td-amount", format:{ func:AmountFormat }},
        {datacolumn:'refTransId', displayText:i18n.lbl_RefTransId, className:"td-id"}
    ];

    const RefreshBtn = (props)=>{
        return(
            <a className="btn btn-icon" data-refresh="false" onClick={props.QueryData}>
                <i className="icon icon-refresh"></i>
                <div className="preloader">
                    <div className="preloader__spiner">
                        <div className="preloader__circles">
                            <div className="preloader__circle-1"></div>
                            <div className="preloader__circle-2"></div>
                        </div>
                    </div>
                </div>
            </a>
        )
    }

    const THead = ()=>{
        const rows = columnDefs.map((col, idx)=>{
            return(
                <div key={`h-col-${col.datacolumn}-${idx}`} className={col.className} data-title={col.datacolumn}>
                    <span className=" text">{col.displayText}</span>
                </div>
            );
        })
        return(
            <div className="thead" role="desktop-only">
                <div className="tr">
                    {rows}
                </div>
            </div>
        )
    }

    const Td = (props)=>{
        return (
            <div className={props.className} data-title={props.dataTitle} role={props.firstElem ? "mobile-click" : ""}>
                <span className="text">{props.displayText}</span>
                { props.firstElem ? <i className="icon icon-caret"></i> : null }
            </div>
        );
    }

    const TBody = (props)=>{
        let dataContainer=[];
        props.Datas.forEach(row => {
            let mDate = moment(row.createTime);
            let sDate = mDate.format("YYYYMMDD");
            if(dataContainer.length==0 || !dataContainer.find(x=>x.Date===sDate)) {
                dataContainer.push({Date:sDate, Data:[row]});
            } else {
                dataContainer[dataContainer.length-1].Data.push(row);
            }
        });

        return dataContainer.map((section, sId)=>{
            let child = section.Data.map((data, dId)=>{
                let tdResult = columnDefs.map((col, cIdx)=>{
                    return (
                        <Td
                            key={`result-${sId}-${dId}-${cIdx}`}
                            className={col.className} 
                            dataTitle={col.displayText} 
                            firstElem={cIdx===0} 
                            displayText={col.format ? col.format.func(data[col.datacolumn], col.format.bindDatacolumn ? data[col.format.bindDatacolumn] : null) : data[col.datacolumn]}
                        />
                    );
                });
                return(
                    <div className="tr" key={`result-${section.Date}-${dId}`}>
                        {tdResult}
                    </div>
                );
            });

            return(
                <div className="tbody" data-date={section.Date} data-open="true" key={`result-${sId}`}>
                    {child}
                </div>
            );
        });
    }

    const ResultTable = (props)=>{
        return props.Datas.length > 0 ? (
            <div className="table table-rwd table-primary">
                <div className="caption">
                    <span className="text"></span>
                    <RefreshBtn QueryData={props.QueryData}/>
                </div>
                <THead />
                <TBody Datas={props.Datas}/>
            </div>
        ) : (
            <div className="no-data">
                <span className="text">{i18n.msg_NoData}</span>
            </div>
        );
    }

    const Panel = ()=>{
        const [stateFilter, setState] = React.useState({
            StartDate:moment().format('YYYY-MM-DD'),
            EndDate:moment().format('YYYY-MM-DD'),
            QuickPick:QuickPickDefs.Today
        });

        const [stateData, setData] = React.useState({
            PageNumber:1,
            TotalPages:0,
            Datas:null
        });

        const QueryData = (pageNumber)=>{            
            Loading.OnLoading();
            let endDate = moment(stateFilter.EndDate).add(1, 'days').unix()-1;
            let data = {StartDate:moment(stateFilter.StartDate).unix(), EndDate: endDate, PageNumber:  pageNumber };
            axios.post('/api/Payment/GetStatement', data)
            .then(function (response) {
                Loading.UnLoading();
                let result = response.data;
                if (result.errorCode == 0) {
                    setData({
                        PageNumber: result.pageNumber, 
                        TotalPages: result.totalPages, 
                        Datas: result.data
                    });
                } else {
                    alert(error);
                }
            }.bind(this)).catch(function (error) {
                Loading.UnLoading();
                alert(error);
            }.bind(this));
        }

        const QuickPickOnSelect = (v, datacolumn)=>{
            let StartDate = '';
            let EndDate = '';

            switch (v.value) {
                case QuickPickDefs.Today.value:
                    StartDate = moment().format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;
                
                case QuickPickDefs.YesterDay.value:
                    StartDate = moment().add(-1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().add(-1, 'days').format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.LastWeek.value:
                    StartDate = moment().subtract(1, 'weeks').startOf('week').format('YYYY-MM-DD');
                    EndDate = moment().subtract(1, 'weeks').endOf('week').format('YYYY-MM-DD');
                
                    break;

                case QuickPickDefs.ThisWeek.value:
                    StartDate = moment().subtract(1, 'weeks').endOf('week').add(1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.ThisMonth.value:
                    StartDate = moment().subtract(1,'months').endOf('month').add(1, 'days').format('YYYY-MM-DD');
                    EndDate = moment().format('YYYY-MM-DD');
                    break;

                case QuickPickDefs.LastMonth.value:
                    StartDate = moment().subtract(1,'months').startOf('month').format('YYYY-MM-DD');
                    EndDate = moment().subtract(1,'months').endOf('month').format('YYYY-MM-DD');
                    break;

                default:
                    StartDate = stateFilter.StartDate;
                    EndDate = stateFilter.EndDate;
                    break;
            }

            setState({
                ...stateFilter,
                StartDate:StartDate,
                EndDate:EndDate,
                [datacolumn]:v
            })
        }

        return(
            <>
                <Header />
                <Filter 
                    {...stateFilter}
                    SetStartDate={(e)=>{setState({...stateFilter, StartDate:e.target.value})}}
                    SetEndDate={(e)=>{setState({...stateFilter, EndDate:e.target.value})}}
                    QueryData={QueryData.bind(null, stateData.PageNumber)}
                    QuickPickOnSelect={QuickPickOnSelect}
                />
                {
                    stateData.Datas === null ? null : (
                        <>
                            <ResultTable QueryData={QueryData.bind(null, stateData.PageNumber)} Datas={stateData.Datas}/>
                            <Pagination onPaging={QueryData} PageNumber={stateData.PageNumber} TotalPages={stateData.TotalPages}/>
                        </>
                    )
                }
            </>
        )
    }

    ReactDOM.render(<Panel/>,document.getElementById('StatementPanel'));
})(jQuery, React, ReactDOM, i18n, axios, moment);