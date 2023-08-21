import {CustInfo} from './CustInfo';

(function ($, React, ReactDOM, i18n, axios, moment) {

    const Panel = ()=>{
        const [stateDatas, updateDatas] = React.useState(null);

        React.useEffect(() => {
            axios.post('/api/AccountStrict/GetCustProfile', {})
            .then(function (response) {
                let result = response.data;
                if (result.errorCode == 0) {                    
                    updateDatas({
                        UserName:result.data.userName,
                        Email:result.data.email,
                        AccountName:result.data.accountName,
                        Birth_d:result.data.birth_d,
                        Birth_m:result.data.birth_m,
                        Birth_y:result.data.birth_y,
                        RegTime:result.data.regTime,
                        LastLogin:result.data.lastLogin,
                        Country:result.data.country,
                        PhoneNo:result.data.phoneNo
                    });
                }else{
                    alert(result.message);
                }
            }).catch(function (error) {
                alert(error);
            });
        }, []);

        return (
            <>
                <div className="heading" role="desktop-only">
                    <span className="text">{i18n.lbl_Profile}</span>
                </div>
                {stateDatas === null ? null : <CustInfo CustInfo={stateDatas}/>}
            </>
        )
    }

    ReactDOM.render(<Panel />, document.getElementById('Profile'));
})(jQuery, React, ReactDOM, i18n, axios, moment);