import areaCode from '../../../commJS/areaCode';

export const CustInfo = (props)=>{
    const columnDefs = [
        { dataColumn:'UserName', columnTitle:i18n.lbl_Username },
        { dataColumn:'Country', columnTitle:i18n.lbl_Country, format:GetDefaultCountry },
        { dataColumn:'PhoneNo', columnTitle:i18n.lbl_Phone, format:phone_format, bindingColumn:['Country']},
        { dataColumn:'Email', columnTitle:i18n.lbl_Email },
        { dataColumn:'AccountName', columnTitle:i18n.lbl_RealName },
        { dataColumn:'Birth_d', columnTitle:i18n.lbl_Birthday, format:birthday_format, bindingColumn:['Birth_m', 'Birth_y'] },
        { dataColumn:'RegTime', columnTitle:i18n.lbl_RegisterTime, format:date_format},
        { dataColumn:'LastLogin', columnTitle:i18n.lbl_LastLogin, format:date_format}
    ];

    let data = props.CustInfo ? props.CustInfo : custInfo;

    return (
        <div className="form--list">
            {
                columnDefs.map((item, idx)=>{
                    let bindingItem = {};
                    item.bindingColumn && item.bindingColumn.forEach(col => {
                        bindingItem[col] = data[col];
                    });
                    return (
                        <div className="form-group" key={`normal-column-${idx}`}>
                            <label className="form-title">{item.columnTitle}：</label>
                            <div className="form-content form-content--text">
                                <span className="text">{item.format ? item.format(data[item.dataColumn], bindingItem) : data[item.dataColumn]}</span>
                            </div>
                        </div>
                    )
                })
            }
        </div>
    );
}

const birthday_format = (v, bindingItem)=>{
    return v===null ? '- / - / -' : `${v}/${bindingItem.Birth_m}/${bindingItem.Birth_y}`
}

const date_format = (v)=>{
    return moment(new Date(v)).format('lll');
}

const phone_format = (v, bindingItem)=>{
    return `(${areaCode[bindingItem.Country].AreaCode})${v}`
}

const GetDefaultCountry = (v)=>{
    let countryInfo = Country.find(x=>x.Code === v);
    return countryInfo.CountryName;
}