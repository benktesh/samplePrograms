package com.XXX.contactupate;
import com.example.testcontact.R;

import android.app.Activity;
import android.content.Context;
import android.os.AsyncTask;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;


/**
 * The main activity is start point for this test application. The user enters the email id of the contact
 * for which he or she wishes to update the phone number for. The interface was designed to mimic the
 * real world application where contact id is selected/chosen by the appication itself. The selection or any other 
 * event will instantiate this activity.
 * 
 * @author Benktesh
 *
 */
public class MainActivity extends Activity {
	private Button button;
	private EditText email;
	private TextView finalResult;
	private Context context;
	
	private static final String TAG = "MainActivity";
 
	@Override
	protected void onCreate(Bundle savedInstanceState) {
		this.context = getApplicationContext(); 
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
  
		email = (EditText) findViewById(R.id.et_email);
		button = (Button) findViewById(R.id.btn_updateContact);
		finalResult = (TextView) findViewById(R.id.tv_result);
		button.setOnClickListener(new View.OnClickListener() {
			@Override
			public void onClick(View v) {
					new GetContactAsync().execute();
			}
		});
	}
 
	private class GetContactAsync extends AsyncTask<Void, Void, String> {
		 
		 @Override
		 protected String doInBackground(Void... params) {
		 Log.e(TAG, "Background processing for contact update started.");
		 ContactUtility cu = new ContactUtility();
		 boolean result = cu.processContactUtilityRequest(context, email.getText().toString());
		 String message = null;
		 if(result)
			 message="Phone number of contact id  " + email.getText().toString()+ " was successfully updated";
		 else
			 message="Contact id " + email.getText().toString()+ " was not  found. Consider inserting?"; 
		 return message;  
	 }
	 
	 protected void onPostExecute(String message) {	 
		 Log.e(TAG, "Background processing of completed.");
		 finalResult.setText(message);
		Toast.makeText(context, "Process Completed", Toast.LENGTH_LONG).show();
	 }
  	}
  }
