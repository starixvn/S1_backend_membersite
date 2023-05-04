import React, { useState, useEffect } from "react";
const Panel = () => {
	const [stateData, updateData] = useState();
	useEffect(() => {
		axios
			.post(`/api/Message/GetAnnouncement/${SiteLanguage}`, {})
			.then(res => {
				let result = res.data.data;
				updateData(
					result
						.filter(item => {
							return item.subject !== null;
						})
						.map((item, idx) => {
							return (
								<Announcement
									key={`announce-${idx}`}
									mainIdx={idx}
									subject={item.subject}
									startTime={moment(item.startTime).format(
										"l"
									)}
									content={item.content}
								/>
							);
						})
				);
			});
	}, []);

	return (
		<>
			<div className="title">{i18n.lbl_Announcement}</div>
			<div className="content promotion-page">{stateData}</div>
		</>
	);
};

const Announcement = props => {
	let content = props.content.split("\\n");
	return (
		<div className="promotion-item">
			<div className="detail">
				<div className="promotion-title">{props.subject}</div>
				<div className="date">{props.startTime}</div>
				<div className="promotion-text">
					{content.map((c, idx) => {
						return (
							<p key={`content-${props.mainIdx}-${idx}`}>{c}</p>
						);
					})}
				</div>
			</div>
		</div>
	);
};

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}
