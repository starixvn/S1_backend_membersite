import * as Loading from '../../commJS/Loading.jsx';

(function ($, React, ReactDOM, i18n, axios, moment) {
    const Panel = ()=>{
        const [stateData, updateData] = React.useState();
        React.useEffect(() => {
            Loading.OnLoading();
            axios.post(`/api/Message/GetAnnouncement/${SiteLanguage}`, {}).then((res)=>{
                Loading.UnLoading();
                let result = res.data.data;
                updateData(result.map((item, idx)=>{
                    return(
                        <Announcement
                            key={`announce-${idx}`}
                            mainIdx={idx}
                            subject={item.subject}
                            startTime={moment(item.startTime).format("l")}
                            content={item.content}
                        />
                    )
                }));
            });
        },[]);
        
        return(
            <>
                <div className="heading">
                    <span className="text">{i18n.lbl_Announcement}</span>
                </div>
                <div className="announcement-list">
                    <div className="announcement-list__body">
                        {stateData}
                    </div>
                </div>
            </>
        )
    }

    const Announcement = (props)=>{
        let content = props.content.split('\\n');
        
        return(
            <div className="announcement">
                <div className="announcement__item">
                    <div className="announcement__title">
                        <span className="text">{props.subject}</span>
                        <span className="text-lowlight">{props.startTime}</span>
                    </div>
                </div>
                <div className="announcement__content">
                    <pre>
                    {
                        content.map((c, idx)=>{
                            return <p key={`content-${props.mainIdx}-${idx}`}>{c}</p>
                        })
                    }
                    </pre>
                </div>
            </div>
        )
    }

    ReactDOM.render(<Panel/>,document.getElementById('Announcement'));
})(jQuery, React, ReactDOM, i18n, axios, moment);